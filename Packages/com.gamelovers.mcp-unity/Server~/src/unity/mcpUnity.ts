import { v4 as uuidv4 } from 'uuid';
import { fileURLToPath } from 'url';
import { Logger } from '../utils/logger.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { UnityConnection, ConnectionState, ConnectionStateChange, UnityConnectionConfig } from './unityConnection.js';
import { CommandQueue, CommandQueueConfig, CommandQueueStats, QueuedCommand } from './commandQueue.js';
import { resolveUnityConnectionConfig } from './unityConnectionConfig.js';

interface PendingRequest {
  resolve: (value: any) => void;
  reject: (reason: any) => void;
  timeout: NodeJS.Timeout;
}

interface UnityRequest {
  id?: string;
  method: string;
  params: any;
}

interface UnityResponse {
  jsonrpc: string;
  id: string;
  result?: any;
  error?: {
    message: string;
    type: string;
    details?: any;
  };
}

/**
 * Connection state change callback type
 */
export type ConnectionStateCallback = (change: ConnectionStateChange) => void;

// Re-export connection types for consumers
export { ConnectionState, type ConnectionStateChange } from './unityConnection.js';
export { type CommandQueueConfig, type CommandQueueStats } from './commandQueue.js';

/**
 * Options for sending a request
 */
export interface SendRequestOptions {
  /** If false, fail before dispatch when disconnected; otherwise wait for transport without queuing/replaying. */
  queueIfDisconnected?: boolean;
  /** Custom timeout for this request in milliseconds */
  timeout?: number;
}

/**
 * Configuration for McpUnity
 */
export interface McpUnityConfig {
  /** Command queue configuration */
  queue?: CommandQueueConfig;
  /** Whether command queuing is enabled by default (default: true) */
  queueingEnabled?: boolean;
}

export class McpUnity {
  private logger: Logger;
  private port: number = 8090;
  private host: string = 'localhost';
  private requestTimeout = 10000;
  private authToken: string = '';
  private startup: Promise<void> | null = null;
  private startupError: McpUnityError | null = null;

  private connection: UnityConnection | null = null;
  private pendingRequests: Map<string, PendingRequest> = new Map<string, PendingRequest>();
  private clientName: string = '';

  // Connection state listeners
  private stateListeners: Set<ConnectionStateCallback> = new Set();

  // Command queue for handling commands during disconnection
  private commandQueue: CommandQueue;
  private queueingEnabled: boolean;

  // Flag to track if we're currently replaying queued commands
  private isReplayingQueue: boolean = false;

  constructor(logger: Logger, config?: McpUnityConfig) {
    this.logger = logger;
    this.commandQueue = new CommandQueue(logger, config?.queue);
    this.queueingEnabled = config?.queueingEnabled ?? true;
  }

  /**
   * Enable or disable command queuing
   */
  public setQueueingEnabled(enabled: boolean): void {
    this.queueingEnabled = enabled;
    this.logger.info(`Command queuing ${enabled ? 'enabled' : 'disabled'}`);
  }

  /**
   * Check if command queuing is enabled
   */
  public get isQueueingEnabled(): boolean {
    return this.queueingEnabled;
  }

  /**
   * Get command queue statistics
   */
  public getQueueStats(): CommandQueueStats {
    return this.commandQueue.getStats();
  }

  /**
   * Get number of commands currently queued
   */
  public get queuedCommandCount(): number {
    return this.commandQueue.size;
  }

  /**
   * Start the Unity connection
   * @param clientName Optional name of the MCP client connecting to Unity
   */
  public start(clientName?: string): Promise<void> {
    this.startup ??= this.startConnection(clientName);
    return this.startup;
  }

  private async startConnection(clientName?: string): Promise<void> {
    try {
      this.logger.info('Attempting to read startup parameters...');
      await this.parseAndSetConfig();

      this.clientName = clientName || '';

      // Create connection with configuration
      const config: UnityConnectionConfig = {
        host: this.host,
        port: this.port,
        requestTimeout: this.requestTimeout,
        authToken: this.authToken,
        clientName: this.clientName,
        // Use defaults for reconnection and heartbeat from UnityConnection
      };

      this.connection = new UnityConnection(this.logger, config);

      // Set up event handlers
      this.connection.on('stateChange', (change: ConnectionStateChange) => {
        this.handleStateChange(change);
      });

      this.connection.on('message', (data: string) => {
        this.handleMessage(data);
      });

      this.connection.on('error', (error: McpUnityError) => {
        this.logger.error(`Connection error: ${error.message}`);
        if (error.type === ErrorType.AUTHENTICATION) {
          this.startupError = error;
          this.commandQueue.clear(error.message);
        }
        // Reject pending requests on connection error
        this.rejectAllPendingRequests(error);
      });

      this.logger.info('Attempting to connect to Unity WebSocket...');
      await this.connection.connect();
      this.startupError = null;
      this.logger.info('Successfully connected to Unity WebSocket');

      if (clientName) {
        this.logger.info(`Client identified to Unity as: ${clientName}`);
      }
    } catch (error) {
      if (!this.connection || error instanceof McpUnityError && error.type === ErrorType.AUTHENTICATION) {
        this.startupError = error instanceof McpUnityError ? error :
          new McpUnityError(ErrorType.CONNECTION, error instanceof Error ? error.message : String(error));
      }
      this.logger.warn(`Could not connect to Unity WebSocket: ${error instanceof Error ? error.message : String(error)}`);
      if (!(error instanceof McpUnityError) || error.type !== ErrorType.AUTHENTICATION) {
        this.logger.warn('Will retry connection on next request (with automatic reconnection)');
      }
    }

    return Promise.resolve();
  }

  /**
   * Reads our configuration file and sets parameters of the server based on them.
   */
  private async parseAndSetConfig() {
    const config = await resolveUnityConnectionConfig(this.logger, {
      modulePath: fileURLToPath(import.meta.url)
    });
    this.port = config.port;
    this.host = config.host;
    this.requestTimeout = config.requestTimeout;
    this.authToken = config.authToken;
  }

  /**
   * Handle connection state changes
   */
  private handleStateChange(change: ConnectionStateChange): void {
    this.logger.debug(`Connection state changed: ${change.previousState} -> ${change.currentState}`);

    // Notify all listeners
    for (const listener of this.stateListeners) {
      try {
        listener(change);
      } catch (err) {
        this.logger.error(`Error in state listener: ${err instanceof Error ? err.message : String(err)}`);
      }
    }

    // Sent commands have an unknown outcome after transport loss; never replay them.
    if (change.previousState === ConnectionState.Connected && change.currentState !== ConnectionState.Connected) {
      this.rejectAllPendingRequests(new McpUnityError(ErrorType.CONNECTION,
        `MCP_TRANSPORT_LOST: ${change.reason || change.currentState}; dispatched outcome unknown; inspect before retry`));
    }
    // Handle specific state transitions
    if (change.currentState === ConnectionState.Connected &&
        (change.previousState === ConnectionState.Reconnecting ||
         change.previousState === ConnectionState.Connecting)) {
      // Connection restored - replay queued commands
      this.replayQueuedCommands();
    } else if (change.currentState === ConnectionState.Disconnected) {
      // Clear the queue when we're fully disconnected (not reconnecting)
      // This happens when max reconnection attempts are reached
      if (change.reason?.includes('Max reconnection attempts')) {
        this.commandQueue.clear(change.reason);
      }
      // Reject all pending requests when disconnected
      this.rejectAllPendingRequests(
        new McpUnityError(ErrorType.CONNECTION, change.reason || 'Connection lost')
      );
    }
  }

  /**
   * Replay queued commands after connection is restored
   */
  private async replayQueuedCommands(): Promise<void> {
    if (this.isReplayingQueue) {
      this.logger.debug('Already replaying queue, skipping');
      return;
    }

    const commands = this.commandQueue.drain();

    if (commands.length === 0) {
      return;
    }

    this.isReplayingQueue = true;
    this.logger.info(`Replaying ${commands.length} queued commands`);

    for (const command of commands) {
      try {
        // Send the command directly using internal method
        const remaining = (command.timeout ?? 60000) - (Date.now() - command.queuedAt);
        if (remaining <= 0) throw new McpUnityError(ErrorType.TIMEOUT, `MCP_NOT_DISPATCHED: ${command.request.method}; queue deadline expired`);
        const result = await this.sendRequestInternal(command.request, remaining);
        command.resolve(result);
        this.commandQueue.recordReplaySuccess();
      } catch (error) {
        command.reject(error);
      }
    }

    this.isReplayingQueue = false;
    this.logger.info(`Finished replaying queued commands (${this.commandQueue.getStats().replayedCount} successful)`);
  }

  /**
   * Handle messages received from Unity
   */
  private handleMessage(data: string): void {
    try {
      const response = JSON.parse(data) as UnityResponse;
      const pendingCount = this.pendingRequests.size;

      if (!response.id) {
        this.logger.warn(`Ignoring Unity WebSocket message without request id; pending requests: ${pendingCount}`);
        return;
      }

      if (!this.pendingRequests.has(response.id)) {
        this.logger.warn(`Ignoring Unity response for unknown request ${response.id}; pending requests: ${pendingCount}`);
        return;
      }

      this.logger.info(`Received Unity response for request ${response.id}; pending requests before match: ${pendingCount}`);
      const request = this.pendingRequests.get(response.id)!;
      clearTimeout(request.timeout);
      this.pendingRequests.delete(response.id);

      if (response.error) {
        request.reject(new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          response.error.message || 'Unknown error',
          response.error.details
        ));
      } else {
        request.resolve(response.result);
      }
    } catch (e) {
      this.logger.error(`Error parsing WebSocket message: ${e instanceof Error ? e.message : String(e)}`);
    }
  }

  /**
   * Reject all pending requests with an error
   */
  private rejectAllPendingRequests(error: McpUnityError): void {
    for (const [id, request] of this.pendingRequests.entries()) {
      clearTimeout(request.timeout);
      request.reject(error);
      this.pendingRequests.delete(id);
    }
  }

  /**
   * Stop the Unity connection and clean up resources
   */
  public async stop(): Promise<void> {
    await this.startup;
    this.startup = null;
    this.startupError = null;
    // Dispose the command queue
    this.commandQueue.dispose();

    if (this.connection) {
      this.connection.disconnect('Server stopping');
      this.connection.removeAllListeners();
      this.connection = null;
    }
    this.rejectAllPendingRequests(new McpUnityError(ErrorType.CONNECTION, 'Server stopped'));
    this.logger.info('Unity WebSocket client stopped');
    return Promise.resolve();
  }

  /**
   * Send a request to the Unity server
   * @param request The request to send
   * @param options Optional settings for the request
   */
  public async sendRequest(request: UnityRequest, options: SendRequestOptions = {}): Promise<any> {
    // The stdio MCP handshake can finish while startup is still reading settings and
    // opening the socket. An early probe must observe the same startup attempt.
    if (!this.connection && this.startup) await this.startup;
    if (this.startupError && (!this.connection || this.startupError.type === ErrorType.AUTHENTICATION)) {
      throw this.startupError;
    }

    const requestId = request.id as string || uuidv4();
    const message: UnityRequest = { ...request, id: requestId };
    const budgetMs = options.timeout ?? this.requestTimeout;
    const deadline = Date.now() + budgetMs;
    if (!this.connection) {
      throw new McpUnityError(ErrorType.CONNECTION, `MCP_NOT_DISPATCHED: ${request.method}; bridge not started`);
    }
    if (!this.isConnected && options.queueIfDisconnected === false) {
      throw new McpUnityError(ErrorType.CONNECTION, `MCP_NOT_DISPATCHED: ${request.method}; state=${this.connectionState}; connection waiting disabled`);
    }
    if (!this.isConnected) {
      await this.waitForConnection(request.method, Math.min(45000, budgetMs));
    }
    const remaining = deadline - Date.now();
    if (remaining <= 0 || !this.isConnected) {
      throw new McpUnityError(ErrorType.CONNECTION, `MCP_NOT_DISPATCHED: ${request.method}; connection wait expired; state=${this.connectionState}`);
    }
    try {
      // Exactly one dispatch. The request's execution budget includes connection waiting.
      return await this.sendRequestInternal(message, remaining);
    } catch (error) {
      // Entering Play Mode / domain reload can close the socket after the editor
      // has already performed the operation. Settle the transport, then expose
      // the unknown outcome for inspection; NEVER replay the original command.
      const isTransportError = error instanceof McpUnityError &&
        (error.type === ErrorType.CONNECTION || error.type === ErrorType.TIMEOUT);
      if (isTransportError && !this.isConnected) {
        try { await this.waitForConnection(request.method, Math.max(0, Math.min(45000, deadline - Date.now()))); }
        catch (recoveryError) {
          if (recoveryError instanceof McpUnityError && recoveryError.type === ErrorType.AUTHENTICATION) throw recoveryError;
          this.logger.warn(`Connection recovery after dispatched ${request.method} is still pending`);
        }
      }
      if (isTransportError && !['get_play_mode_status', 'get_scene_info', 'get_console_logs', 'get_operation_result', 'recover_test_operation'].includes(request.method)) {
        const recovered = await this.recoverOperation(requestId, Math.min(120000, Math.max(5000, budgetMs)));
        if (recovered?.status === 'completed' && recovered.result) return recovered.result;
        if (recovered?.status === 'interrupted') {
          throw new McpUnityError(ErrorType.CONNECTION,
            `MCP_RESULT_UNKNOWN: ${request.method}; operationId=${requestId}; editor domain reloaded before a result was recorded. Inspect actual test artifacts/state; do not replay automatically.`);
        }
      }
      if (isTransportError) {
        throw new McpUnityError(error.type,
          `MCP_RESULT_UNKNOWN: ${request.method}; dispatched outcome unknown; transport=${this.connectionState}; inspect editor state before retry. ${error.message}`);
      }
      throw error;
    }
  }

  /** Poll durable receipts internally, without replaying the original command. */
  private async recoverOperation(operationId: string, budgetMs: number): Promise<any | undefined> {
    const deadline = Date.now() + budgetMs;
    while (Date.now() < deadline) {
      try {
        if (!this.isConnected) await this.waitForConnection('get_operation_result', Math.min(5000, deadline - Date.now()));
        const result = await this.sendRequestInternal({ id: uuidv4(), method: 'get_operation_result', params: { operationId } }, Math.min(5000, Math.max(1, deadline - Date.now())));
        if (result?.status === 'completed' || result?.status === 'interrupted') return result;
        if (result?.status !== 'running') return undefined; // old bridge or no durable receipt
      } catch (error) {
        if (error instanceof McpUnityError && error.type === ErrorType.AUTHENTICATION) throw error;
      }
      const delay = Math.min(1000, deadline - Date.now());
      if (delay > 0) await new Promise<void>(resolve => setTimeout(resolve, delay));
    }
    return undefined;
  }

  /** Wait only for transport; never enqueue or replay editor commands. */
  private async waitForConnection(method: string, limitMs: number): Promise<void> {
    const deadline = Date.now() + limitMs;
    this.logger.info(`Waiting for Unity transport before continuing ${method}`);
    while (!this.isConnected) {
      if (this.startupError?.type === ErrorType.AUTHENTICATION) throw this.startupError;
      if (!this.connection || Date.now() >= deadline) {
        throw new McpUnityError(ErrorType.CONNECTION,
          `MCP_NOT_DISPATCHED: ${method}; connection wait expired; state=${this.connectionState}`);
      }
      if (!this.connection.isConnecting) void this.connection.connect().catch((error: unknown) => {
        if (error instanceof McpUnityError && error.type === ErrorType.AUTHENTICATION) this.startupError = error;
      });
      if (!this.isConnected) await new Promise<void>(resolve => setTimeout(resolve, Math.min(250, Math.max(1, deadline - Date.now()))));
    }
    if (this.startupError?.type === ErrorType.AUTHENTICATION) throw this.startupError;
    this.logger.info(`Unity transport restored for ${method}`);
  }

  /**
   * Internal method to send a request directly to Unity
   * Bypasses queuing logic - assumes connection is already established
   */
  private sendRequestInternal(request: UnityRequest, customTimeout?: number): Promise<any> {
    const requestId = request.id as string;
    const timeoutMs = request.method === "get_play_mode_status" ? Math.min(customTimeout ?? this.requestTimeout, 5000) : customTimeout ?? this.requestTimeout;

    return new Promise((resolve, reject) => {
      if (!this.connection || !this.isConnected) {
        reject(new McpUnityError(ErrorType.CONNECTION, 'Not connected to Unity'));
        return;
      }

      // Create timeout for the request
      const timeout = setTimeout(() => {
        if (this.pendingRequests.has(requestId)) {
          this.logger.error(`Request ${requestId} timed out after ${timeoutMs}ms`);
          this.pendingRequests.delete(requestId);
          reject(new McpUnityError(ErrorType.TIMEOUT, `MCP_RESULT_UNKNOWN: ${request.method}; id=${requestId}; timeout=${timeoutMs}ms; transportConnected=${this.isConnected}; editor may be busy; inspect before retry`));
        }
      }, timeoutMs);

      // Store pending request
      const pendingBeforeSend = this.pendingRequests.size;
      this.pendingRequests.set(requestId, {
        resolve,
        reject,
        timeout
      });

      try {
        this.logger.info(`Sending Unity request ${requestId} (${request.method}) while connection state is ${this.connectionState}; pending requests before send: ${pendingBeforeSend}`);
        this.connection.send(JSON.stringify({ ...request, _deadlineUtcMs: Date.now() + timeoutMs }));
        this.logger.debug(`Request sent: ${requestId}`);
      } catch (err) {
        clearTimeout(timeout);
        this.pendingRequests.delete(requestId);
        reject(new McpUnityError(ErrorType.CONNECTION, `Send failed: ${err instanceof Error ? err.message : String(err)}`));
      }
    });
  }

  /**
   * Check if connected to Unity
   * Only returns true if the connection is guaranteed to be active
   */
  public get isConnected(): boolean {
    return this.connection !== null && this.connection.isConnected;
  }

  /**
   * Get current connection state
   */
  public get connectionState(): ConnectionState {
    return this.connection?.connectionState ?? ConnectionState.Disconnected;
  }

  /**
   * Check if currently connecting or reconnecting
   */
  public get isConnecting(): boolean {
    return this.connection?.isConnecting ?? false;
  }

  /**
   * Add a listener for connection state changes
   * @param callback Function to call when connection state changes
   * @returns Function to remove the listener
   */
  public onConnectionStateChange(callback: ConnectionStateCallback): () => void {
    this.stateListeners.add(callback);
    return () => {
      this.stateListeners.delete(callback);
    };
  }

  /**
   * Force a reconnection to Unity
   * Useful when Unity has reloaded and the connection may be stale
   */
  public forceReconnect(): void {
    if (this.connection) {
      this.connection.forceReconnect();
    } else {
      this.logger.warn('Cannot force reconnect - not started');
    }
  }

  /**
   * Get connection statistics
   */
  public getConnectionStats(): {
    state: ConnectionState;
    pendingRequests: number;
    reconnectAttempt?: number;
    timeSinceLastPong?: number;
  } {
    const stats = this.connection?.getStats();
    return {
      state: stats?.state ?? ConnectionState.Disconnected,
      pendingRequests: this.pendingRequests.size,
      reconnectAttempt: stats?.reconnectAttempt,
      timeSinceLastPong: stats?.timeSinceLastPong
    };
  }

}
