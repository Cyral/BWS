using System;
using System.IO;

namespace BinaryWebSockets {
    public class OutgoingMessage {
        private readonly BinaryWriter binaryWriter;
        private byte[] data;
        private bool writable;

        internal OutgoingMessage() {
            writable = true;
            binaryWriter = new BinaryWriter(new MemoryStream());
        }

        public void WriteString(string value) {
            CheckWritable();
            binaryWriter.Write(value);
        }

        public void WriteInt32(int value) {
            CheckWritable();
            binaryWriter.Write(value);
        }

        public void WriteInt16(short value) {
            CheckWritable();
            binaryWriter.Write(value);
        }

        public void WriteInt8(sbyte value) {
            CheckWritable();
            binaryWriter.Write(value);
        }

        public void WriteUInt32(uint value) {
            CheckWritable();
            binaryWriter.Write(value);
        }

        public void WriteUInt16(ushort value) {
            CheckWritable();
            binaryWriter.Write(value);
        }

        public void WriteUInt8(byte value) {
            CheckWritable();
            binaryWriter.Write(value);
        }

        public void WriteDouble(double value) {
            CheckWritable();
            binaryWriter.Write(value);
        }

        public void WriteFloat(float value) {
            CheckWritable();
            binaryWriter.Write(value);
        }

        public void WriteBoolean(bool value) {
            CheckWritable();
            binaryWriter.Write(value);
        }

        public void WriteBytes(byte[] array) {
            CheckWritable();
            binaryWriter.Write(array);
        }

        internal byte[] GetData() {
            if (writable) {
                writable = false;
                data = ((MemoryStream) binaryWriter.BaseStream).ToArray();
                binaryWriter.Dispose();
            }

            return data;
        }

        private void CheckWritable() {
            if (!writable)
                throw new InvalidOperationException("Message is not writable. It may have already been sent.");
        }
    }
}