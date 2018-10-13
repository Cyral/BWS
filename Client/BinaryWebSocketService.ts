import {Buffer} from "buffer";
import {OutgoingMessage} from "./OutgoingMessage";
import {IncomingMessage} from "./IncomingMessage";
import {BWSOptions} from "./BWSOptions";

export class BinaryWebSocketService {

    /**
     * Function to be called when the client is connected.
     */
    public connected: () => void;

    /**
     * Function to be called when the client is disconnected.
     */
    public disconnected: (event: CloseEvent) => void;

    /**
     * Function to be called when the client receives a message.
     */
    public messageReceived: (event: IncomingMessage) => void;

    private socket: WebSocket;
    private options: BWSOptions;

    constructor(options: BWSOptions) {
        this.options = options;
    }

    /**
     * Connects to the server and waits until the client is connected.
     */
    public async connect(): Promise<void> {
        this.socket = new WebSocket(this.options.connectionURL, ['bws']);
        this.socket.binaryType = 'arraybuffer';

        this.socket.addEventListener('open', this.onOpen.bind(this));
        this.socket.addEventListener('message', this.onMessage.bind(this));
        this.socket.addEventListener('close', this.onClose.bind(this));

        await new Promise(accept => {
            this.socket.addEventListener('open', accept);
        });
    }

    /**
     * Creates a new outgoing message.
     */
    public createMessage(): OutgoingMessage {
        return new OutgoingMessage();
    }

    /**
     * Sends a message to the server.
     */
    public send(om: OutgoingMessage): void {
        if (this.socket && this.socket.readyState === WebSocket.OPEN) {
            this.socket.send(om.getData());
        }
    }

    /**
     * Disconnects from the server.
     */
    public async disconnect(): Promise<void> {
        if (this.socket) {
            await new Promise(accept => {
                this.socket.addEventListener('close', accept);
                this.socket.close();
            });
        }
    }

    private onMessage(event: MessageEvent): void {
        const buffer = new Buffer(event.data);
        const inc = new IncomingMessage(buffer);
        if (this.messageReceived)
            this.messageReceived(inc);
    }

    private onOpen(): void {
        if (this.connected)
            this.connected();
    }

    private onClose(event: CloseEvent): void {
        if (this.disconnected)
            this.disconnected(event);
    }
}