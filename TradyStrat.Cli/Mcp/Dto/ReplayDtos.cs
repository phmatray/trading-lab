// TradyStrat.Cli/Mcp/Dto/ReplayDtos.cs
// The MCP get_replay_report tool returns
// TradyStrat.Application.AiSuggestion.UseCases.ReplayReport directly.
// No MCP-specific DTO is defined here — the existing record was designed
// for external display via the `replay` CLI command and is reused as-is.
//
// If a future change needs a different shape on the wire, define it here
// and add a mapper Strategy in TradyStrat.Cli/Mcp/Mapping/.

namespace TradyStrat.Cli.Mcp.Dto;
