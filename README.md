# BWS (Binary WebSockets)

This library provides a TypeScript client and ASP.Net Core server for binary WebSocket communication. Messages are read and written using helper methods to read individual values. 

This library does not make many assumptions on how your data will be serialized so it is very barebones by design. You will need to create a system for serializing/deserializing messages, differentiating between message types, and keeping track of connected clients.

BWS uses BinaryWriter and BinaryReader internally on the server side, and a compatible implementation on the client side based on Node.JS buffers. Incoming and outgoing messages are represented as objects with methods such as `WriteString`, `WriteInt32`, etc. which allows a custom serialization specification.

## Installation:

- **Client**: [`npm install binary-websockets`](https://www.npmjs.com/package/binary-websockets)
- **Server**: Build and import .dll from project

## Example:
Below a minimal example that uses this library. It uses classes to define different types of messages, where the first byte of each message determines the type.

### Client Side:

App.ts (imports omitted)
```ts
export class App {
  public websockets: BinaryWebSocketService;

  constructor(container: HTMLElement) {
    super(container);

    this.websockets = new BinaryWebSocketService({
      connectionURL: "wss://live.example.localhost/ws"
    });
    
    this.connect();
  }
    
  private async connect() {
    this.websockets.messageReceived = this.onMessage.bind(this);

    try {
      await this.websockets.connect();
    } catch (e) {
      console.error("Error connecting", e);
    }
    
    this.send(new ChatMessage("Test message!"));
  }

  private onMessage(inc: IncomingMessage) {
    const type = <MessageType>inc.readUInt8();

    if (type == MessageType.Chat) {
      const msg = this.decode<ChatMessage>(inc, ChatMessage);
      console.log("Received chat message: " + msg.message, msg.data);
    }
  }
  
  private send(message: IMessage) {
    const om = this.websockets.createMessage();
    om.writeUInt8(message.type);
    message.encode(om);
    this.websockets.send(om);
  }
  
  private decode<T extends IMessage>(inc: IncomingMessage, messageType: new (...params: any[]) => T): T {
    const message = <T>new messageType(undefined);
    message.decode(inc);
    return message;
  }
}
```

Messages/MessageType.ts:
```ts
export enum MessageType {
    Chat = 1,
    Join = 2,
    Move = 3,
}
```

Messages/IMessage.ts:
```ts
export interface IMessage {
  readonly type: MessageType;
  encode(om : OutgoingMessage): void
  decode(im: IncomingMessage): void 
}
```

Messages/ChatMessage.ts:
```ts
export class ChatMessage implements IMessage {
  public message: string;

  public readonly type: MessageType = MessageType.Chat;

  constructor(message: string) {
    // If the first param is undefined, we are trying to decode the message and just need to create an empty instance of it.
    if (arguments[0] === undefined) return;
    this.message = message;
  }

  decode(im: IncomingMessage): void {
    this.message = im.readString();
  }

  encode(om: OutgoingMessage): void {
    om.writeString(this.message);
  }
}
```

### Server Side:

Startup.cs (namespaces and usings omitted)
```cs
public class Startup {
  public void ConfigureServices(IServiceCollection services) {
    services.AddBinaryWebSockets();
  }

  public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
    app.UseBinaryWebSockets(new BWSOptions() {
        AllowedOrigins = {"https://example.localhost"},
        Path = "/ws"
    });

    ActivatorUtilities.CreateInstance<App>(app.ApplicationServices);
  }
```

App.cs
```cs
public class App {
  private NetworkComponent networkComponent;
  private List<Connection> connections;
  
  public App(NetworkComponent networkComponent) {
    connections = new List<Connection>();
    this.networkComponent = networkComponent;

    networkComponent.ClientConnected += async connection => {
      connections.Add(connection);
    };

    networkComponent.ClientDisconnected += connection => {
      connections.Remove(connection);
    };
    
    networkComponent.MessageReceived += async (connection, inc) => {
      var type = (MessageType) inc.ReadUInt8();
      switch (type) {
        case MessageType.Chat: {
          var msg = new ChatMessage(inc);

          Console.WriteLine($"Received chat message: {msg.Message}");
          Console.WriteLine(string.Join(", ", msg.Data));

          var res = new ChatMessage("A response message!");
          await Send(res, connection);

          break;
        }
      }

      networkComponent.Recycle(inc);
    };
  }
  
  public async Task Send(IMessage message, Connection to) {
    var om = EncodeMessage(message);
    await networkComponent.Send(om, to);
  }

  public async Task Broadcast(IMessage message) {
    var om = EncodeMessage(message);
    foreach (var connection in connections) {
      await networkComponent.Send(om, connection);
    }
  }

  public async Task BroadcastExcept(IMessage message, Connection except) {
    var om = EncodeMessage(message);
    foreach (var connection in connections) {
      if (connection != except) {
        await networkComponent.Send(om, connection);
      }
    }
  }

  public OutgoingMessage EncodeMessage(IMessage message) {
    var om = networkComponent.CreateMessage();
    om.WriteUInt8((byte) message.Type);
    message.Encode(om);
    return om;
  }   
}

```

MessageType.cs
```cs
public enum MessageType {
  Chat = 1,
  Join = 2,
  Move = 3,
}
```

IMessage.cs
```cs
public interface IMessage {
  MessageType Type { get; }
  void Encode(OutgoingMessage om);
  void Decode(IncomingMessage im);
}
```

MessageType.cs
```cs
public class ChatMessage : IMessage {
  public string Message { get; private set; }

  public MessageType Type => MessageType.Chat;

  public ChatMessage(string message) {
    Message = message;
  }

  public ChatMessage(IncomingMessage im) {
    Decode(im);
  }

  public void Decode(IncomingMessage im) {
    Message = im.ReadString();
  }

  public void Encode(OutgoingMessage om) {
    om.WriteString(Message);
  }
}
```
