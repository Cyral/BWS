using System;
using System.IO;
using System.Text;

namespace BinaryWebSockets {
    public class IncomingMessage {
        
        public Connection Connection { get; private set; }

        private BinaryReader binaryReader;
        
        internal IncomingMessage(BinaryReader binaryReader, Connection connection) {
            this.binaryReader = binaryReader;
            Connection = connection;
        }
        
        public string ReadString() {
            return binaryReader.ReadString();
        }
        
        public int ReadInt32() {
            return binaryReader.ReadInt32();
        }
        
        public short ReadInt16() {
            return binaryReader.ReadInt16();
        }
        
        public sbyte ReadInt8() {
            return binaryReader.ReadSByte();
        }
        
        public uint ReadUInt32() {
            return binaryReader.ReadUInt32();
        }
        
        public ushort ReadUInt16() {
            return binaryReader.ReadUInt16();
        }
        
        public byte ReadUInt8() {
            return binaryReader.ReadByte();
        }
        
        public double ReadDouble() {
            return binaryReader.ReadDouble();
        }
        
        public float ReadFloat() {
            return binaryReader.ReadSingle();
        }

        public bool ReadBoolean() {
            return binaryReader.ReadBoolean();
        }

        public byte[] ReadBytes(int count) {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
            return binaryReader.ReadBytes(count);
        }

        internal void Dispose() {
            binaryReader.Dispose();
        }
    }
}