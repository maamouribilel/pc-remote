using System.Net.WebSockets;
using System.Text.Json;
using PcController.Core;

internal static class SocketReplies
{
    internal static async Task<JsonDocument> Json(ClientWebSocket socket, CancellationToken token, int chunkSize = 1024)
    {
        var bytes = new byte[ControlProtocol.MaxReplyBytes + 1]; var length = 0;
        ValueWebSocketReceiveResult reply;
        do
        {
            var room = Math.Min(chunkSize, bytes.Length - length);
            if (room <= 0) throw new InvalidDataException("Server reply exceeds its bound.");
            reply = await socket.ReceiveAsync(bytes.AsMemory(length, room), token);
            if (reply.MessageType != WebSocketMessageType.Text) throw new WebSocketException("Expected text reply.");
            length += reply.Count;
            if (length > ControlProtocol.MaxReplyBytes) throw new InvalidDataException("Server reply exceeds its bound.");
        } while (!reply.EndOfMessage);
        return JsonDocument.Parse(bytes.AsMemory(0, length), new JsonDocumentOptions { MaxDepth = 8 });
    }
}
