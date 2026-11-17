import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve, isAbsolute } from 'node:path';
import { randomUUID } from 'node:crypto';
import * as z from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { CallToolResult } from "@modelcontextprotocol/sdk/types.js";

// Constants for the tool
const toolName = "get_console_logs";
const toolDescription = "Retrieves logs from the Unity console with pagination support to avoid token limits";
const paramsSchema = z.object({
  logType: z
    .enum(["info", "warning", "error"])
    .optional()
    .describe(
      "The type of logs to retrieve (info, warning, error) - defaults to all logs if not specified"
    ),
  offset: z
    .number()
    .int()
    .min(0)
    .optional()
    .describe("Starting index for pagination (0-based, defaults to 0)"),
  limit: z
    .number()
    .int()
    .min(1)
    .max(500)
    .optional()
    .describe("Maximum number of logs to return (defaults to 50, max 500 to avoid token limits)"),
  includeStackTrace: z
    .boolean()
    .optional()
    .describe("Whether to include stack trace in logs. ⚠️ ALWAYS SET TO FALSE to save 80-90% tokens, unless you specifically need stack traces for debugging. Default: true (except info logs in resource)")
});

/**
 * Creates and registers the Get Console Logs tool with the MCP server
 * This tool allows retrieving messages from the Unity console
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerGetConsoleLogsTool(
  server: McpServer,
  mcpUnity: McpUnity,
  logger: Logger
) {
  logger.info(`Registering tool: ${toolName}`);

  // Register this tool with the MCP server
  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: z.infer<typeof paramsSchema>) => {
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

/**
 * Handles requests for Unity console logs
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(
  mcpUnity: McpUnity,
  params: z.infer<typeof paramsSchema>
): Promise<CallToolResult> {
  const { logType, offset = 0, limit = 20, includeStackTrace = false } = params;

  // Send request to Unity using the same method name as the resource
  // This allows reusing the existing Unity-side implementation
  const response = await mcpUnity.sendRequest({
    method: "get_console_logs",
    params: {
      logType: logType,
      offset: offset,
      limit: limit,
      includeStackTrace: includeStackTrace,
    },
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || "Failed to fetch logs from Unity"
    );
  }

  const logs = response.data ?? response.logs ?? response;

  return {
    content: [
      {
        type: "text",
        text: boundedOutput(logs),
      },
    ],
    data: {
      offset,
      limit,
      logType,
      includeStackTrace,
    },
  };
}

/** Preserve the complete output before returning a bounded, explicitly partial preview. */
function boundedOutput(payload: unknown): string {
  const text = JSON.stringify(payload, null, 2);
  if (text.length <= 12000) return text;
  const settings = process.env.MCP_UNITY_SETTINGS_PATH;
  // Never truncate without a durable full result. If archiving fails, preserve original output.
  if (!settings || !isAbsolute(settings)) return text;
  try {
    const folder = resolve(dirname(settings), '..', 'Library', 'UnityOrchestrator', 'ToolResults');
    mkdirSync(folder, { recursive: true });
    const file = join(folder, randomUUID() + '.json');
    writeFileSync(file, text, { encoding: 'utf8', flag: 'wx' });
    return JSON.stringify({ truncated: true, totalCharacters: text.length, fullResultPath: file,
      notice: 'Partial preview only. Read the saved result in bounded ranges for remaining evidence/errors.', preview: text.slice(0, 8000) });
  } catch { return text; }
}
