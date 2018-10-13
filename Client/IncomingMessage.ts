import {Buffer} from "buffer";

export class IncomingMessage {
    
    private index: number = 0;
    private buffer: Buffer;
    
    constructor(buffer: Buffer) {
        this.buffer = buffer;
    }

    public readString(): string {
        const length = this.read7BitEncodedInt();
        const value = this.buffer.toString('utf8', this.index, this.index + length);
        this.index += length;
        return value;
    }

    public readInt32(): number {
        const value = this.buffer.readInt32LE(this.index);
        this.index += 4;
        return value;
    }

    public readInt16(): number {
        const value = this.buffer.readInt32LE(this.index);
        this.index += 2;
        return value;
    }

    public readInt8(): number {
        const value = this.buffer.readInt8(this.index);
        this.index += 1;
        return value;
    }

    public readUInt32(): number {
        const value = this.buffer.readUInt32LE(this.index);
        this.index += 4;
        return value;
    }

    public readUInt16(): number {
        const value = this.buffer.readUInt16LE(this.index);
        this.index += 2;
        return value;
    }

    public readUInt8(): number {
        const value = this.buffer.readUInt8(this.index);
        this.index += 1;
        return value;
    }
    
    public readDouble(): number {
        const value = this.buffer.readDoubleLE(this.index);
        this.index += 8;
        return value;
    }

    public readFloat(): number {
        const value = this.buffer.readFloatLE(this.index);
        this.index += 4;
        return value;
    }
    
    public readBoolean(): boolean {
        const value = this.buffer.readUInt8(this.index);
        this.index += 1;
        return value === 0;
    }
    
    public readBytes(count: number): Uint8Array {
        if (count <= 0)
            throw new Error("Count must be greater than zero.");
        const bytes = this.buffer.slice(this.index, this.index + count);
        this.index += count;
        return bytes;
    }

    /**
     * Read an integer encoded as a sequence bytes where 7-bits represent the value, and the highest bit represents
     * if the value continues into the next byte. This is how .NET's BinaryWriter encodes the string length.
     */
    private read7BitEncodedInt(): number {
        // Read an Int32 7 bits at a time, where the high bit of the byte is set if we should continue reading more bytes.
        var count = 0;
        var shift = 0;
        var b;
        do {
            // Check for a corrupted stream. Read a max of 5 bytes.
            if (shift == 5 * 7) // 5 bytes max per Int32, shift += 7
                throw new Error("Invalid format.");

            b = this.buffer.readUInt8(this.index);
            this.index++;
            count |= (b & 127) << shift;
            shift += 7;
        } while ((b & 128) != 0);
        return count;
    }
}