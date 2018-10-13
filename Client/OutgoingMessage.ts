import {Buffer} from "buffer";

export class OutgoingMessage {

    private writable: boolean = true;
    private index: number = 0;
    private buffer: Buffer;

    constructor() {
        this.buffer = new Buffer(64);
    }

    public writeString(value: string): void {
        this.checkWritable();
        const length = Buffer.byteLength(value, "utf8");
        this.write7BitEncodedInt(length);
        this.ensureCapacity(length);
        this.buffer.write(value, this.index, length, "utf8");
        this.index += length;
    }

    public writeInt32(value: number): void {
        this.checkWritable();
        this.ensureCapacity(4);
        this.buffer.writeInt32LE(value, this.index);
        this.index += 4;
    }

    public writeInt16(value: number): void {
        this.checkWritable();
        this.ensureCapacity(2);
        this.buffer.writeInt16LE(value, this.index);
        this.index += 2;
    }

    public writeInt8(value: number): void {
        this.checkWritable();
        this.ensureCapacity(1);
        this.buffer.writeInt8(value, this.index);
        this.index += 1;
    }

    public writeIUnt32(value: number): void {
        this.checkWritable();
        this.ensureCapacity(4);
        this.buffer.writeUInt32LE(value, this.index);
        this.index += 4;
    }

    public writeUInt16(value: number): void {
        this.checkWritable();
        this.ensureCapacity(2);
        this.buffer.writeUInt16LE(value, this.index);
        this.index += 2;
    }

    public writeUInt8(value: number): void {
        this.checkWritable();
        this.ensureCapacity(1);
        this.buffer.writeUInt8(value, this.index);
        this.index += 1;
    }

    public writeDouble(value: number): void {
        this.checkWritable();
        this.ensureCapacity(8);
        this.buffer.writeFloatLE(value, this.index);
        this.index += 8;
    }

    public writeFloat(value: number): void {
        this.checkWritable();
        this.ensureCapacity(4);
        this.buffer.writeFloatLE(value, this.index);
        this.index += 4;
    }

    public writeBoolean(value: boolean): void {
        this.checkWritable();
        this.ensureCapacity(1);
        this.buffer.writeUInt8(value ? 1 : 0, this.index);
        this.index += 1;
    }

    public writeBytes(bytes: Uint8Array): void {
        this.checkWritable();
        const previousData = this.toBuffer();
        // Merge the existing data with the new byte array to be written.
        this.buffer = Buffer.concat([previousData, Buffer.from(bytes.buffer)], this.index + bytes.byteLength);
        this.index += bytes.byteLength;
    }

    /**
     * Returns a buffer with the message's data.
     * The buffer size will be exactly the size needed to store the message.
     */
    public getData(): Buffer {
        this.writable = false;
        return this.toBuffer();
    }
    
    private toBuffer(): Buffer {
        // Copy the potentially larger buffer to a buffer that is exactly the message size so we aren't 
        // sending extra/useless data.
        if (this.buffer.length !== this.index) {
            this.buffer =  this.buffer.slice(0, this.index);
        }
        return this.buffer;
    }
    
    private checkWritable(): void {
        if (!this.writable)
            throw new Error("Message is not writable. It may have already been sent.");
    }

    /**
     * Write an integer encoded as a sequence bytes where 7-bits represent the value, and the highest bit represents
     * if the value continues into the next byte. This is how .NET's BinaryWriter encodes the string length.
     */
    private write7BitEncodedInt(value: number): void {
        var v = value;
        // Write an Int32 7 bits at a time, where the highest bit is set to a one if the value will continue into another byte.
        while (v >= 128) {
            // Convert the value to a byte, with the highest bit set to 1.
            this.writeUInt8((v % 256) | 128);
            v = v >> 7;
        }
        // Write the remainder.
        this.writeUInt8(v);
    }

    /**
     * Checks that the internal buffer can store the requested number of bytes to write to it. If not, the buffer is
     * resized to fit at least the amount of data requested.
     * @param additionalBytes The number of bytes requested to write to the buffer.
     */
    private ensureCapacity(additionalBytes: number): void {
        let requestedSize = this.index + additionalBytes;
        if (requestedSize > this.buffer.length) {
            let newSize = requestedSize;

            // Make the new buffer at least twice the size of the previous one.
            if (newSize < this.buffer.length * 2)
                newSize = this.buffer.length * 2;

            this.resizeBuffer(newSize);
        }
    }

    private resizeBuffer(size: number): void {
        const extendBy = size - this.buffer.length;
        const extendedBuffer = new Buffer(extendBy);
        this.buffer = Buffer.concat([this.buffer, extendedBuffer], size);
    }
}