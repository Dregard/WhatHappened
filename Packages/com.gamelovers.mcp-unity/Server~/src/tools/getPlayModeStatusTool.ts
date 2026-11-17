import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'get_play_mode_status';
const toolDescription = 'Gets Unity play mode status (isPlaying, isPaused).';

const paramsSchema = z.object({});

export function registerGetPlayModeStatusTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  server.tool(
    'get_operation_result', 'Inspect a durable managed operation receipt; never replays it.',
    { operationId: z.string() },
    async (params: any): Promise<CallToolResult> => {
      const response = await mcpUnity.sendRequest({ method: 'get_operation_result', params }, { timeout: 10000, queueIfDisconnected: false });
      return { structuredContent: response, content: [{ type: 'text', text: JSON.stringify(response) }] };
    }
  );
  server.tool(
    'recover_test_operation', 'Inspect or resolve an orphaned test receipt ONLY when TestRunnerApi proves no test is active and the editor is idle. Empty id selects the pending test. Never cancels tests, replays operations, or asserts a missing result passed.',
    { action: z.enum(['inspect', 'resolve_inactive']), operationId: z.string() },
    async (params: any): Promise<CallToolResult> => {
      const response = await mcpUnity.sendRequest({ method: 'recover_test_operation', params }, { timeout: 15000, queueIfDisconnected: false });
      return { structuredContent: response, isError: response.success !== true, content: [{ type: 'text', text: JSON.stringify(response) }] };
    }
  );
  server.tool(
    'cleanup_temporary_assets',
    'Delete an exclusive sealed orchestrator temporary asset scope. Preserves changed, referenced, dirty or active assets.',
    { scopeId: z.string(), files: z.record(z.string()) },
    async (params: any): Promise<CallToolResult> => {
      const response = await mcpUnity.sendRequest({ method: 'cleanup_temporary_assets', params }, { timeout: 120000, queueIfDisconnected: false });
      return { structuredContent: { deleted: response.deleted === true, reason: response.reason },
        isError: response.deleted !== true,
        content: [{ type: 'text', text: JSON.stringify(response) }] };
    }
  );
  server.tool(
    'reload_active_scene',
    'Capture active scene before editing, or reload it without a save dialog. Dirty editor and disk states are backed up; other loaded scenes are preserved. Include any returned warning in the task report.',
    { action: z.enum(['capture', 'reload']), expectedScenePath: z.string(), expectedSceneHandle: z.number(), expectedDiskSha256: z.string(), expectedEditorWasDirty: z.boolean() },
    async (params: any): Promise<CallToolResult> => {
      const response = await mcpUnity.sendRequest({ method: 'reload_active_scene', params }, { timeout: 120000, queueIfDisconnected: false });
      return { structuredContent: response, isError: response.success !== true, content: [{ type: 'text', text: JSON.stringify(response) }] };
    }
  );
  logger.info(`Registering tool: ${toolName}`);

  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);
        const result = await toolHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${toolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${toolName}`, error);
        throw error;
      }
    }
  );
}

async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: toolName,
    params
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get play mode status'
    );
  }

  const statusText = response.isPlaying
    ? (response.isPaused ? 'Play mode (paused)' : 'Play mode')
    : 'Edit mode';

  return {
    structuredContent: { orchestratorReadiness: response.orchestratorReadiness },
    content: [
      {
        type: response.type as 'text',
        text: statusText + "\n" + JSON.stringify({ orchestratorReadiness: response.orchestratorReadiness })
      }
    ],
    data: {
      isPlaying: response.isPlaying,
      isPaused: response.isPaused
    }
  };
}
