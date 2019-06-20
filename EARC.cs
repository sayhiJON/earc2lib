using System;
using System.Collections.Generic;
using System.Text;

//* non-default
using System.IO;
using Joveler.ZLibWrapper;
using System.Security.Cryptography;
using System.Diagnostics;

namespace earc2 {
    internal static class Extensions {
        public static string ReadCString(this BinaryReader reader) {
            StringBuilder builder = new StringBuilder();

            char character;

            while ((character = reader.ReadChar()) != 0)
                builder.Append(character);

            return builder.ToString();
        }
    }

    public static class EARC {
        public const long				MasterArchiveKeyA			= unchecked((Int64)0xCBF29CE484222325),
                                        MasterArchiveKeyB			= unchecked(0x40D4CCA269811DAF),
                                        MasterFileHash				= unchecked(0x14650FB0739D0383),
                                        MasterFileKey				= unchecked(0x100000001B3),
                                        MasterChunkKeyA				= unchecked(0x10E64D70C2A29A69),
                                        MasterChunkKeyB				= unchecked(0xC63D3dC167E);
        //* 8B265046EDA33E8A

        public const uint				FLAGS_ARCHIVE_HASLOOSEDATA	= 0x1,
                                        FLAGS_ARCHIVE_HASLOCALEDATA = 0x2,
                                        FLAGS_ARCHIVE_DEBUGARCHIVE	= 0x4,
                                        FLAGS_ARCHIVE_ENCRYPTED		= 0x8;

        public const uint				FLAGS_FILE_AUTOLOAD			= 0x1,
                                        FLAGS_FILE_COMPRESSED		= 0x2,
                                        FLAGS_FILE_REFERENCE		= 0x4,
                                        FLAGS_FILE_NOEARC			= 0x8,
                                        FLAGS_FILE_PATCHED			= 0x10,
                                        FLAGS_FILE_PATCHED_DELETED	= 0x20,
                                        FLAGS_FILE_ENCRYPTED		= 0x40;

        public static readonly byte[]	AESKey				= new byte[] { 0x9C, 0x6C, 0x5D, 0x41, 0x15, 0x52, 0x3F, 0x17, 0x5A, 0xD3, 0xF8, 0xB7, 0x75, 0x58, 0x1E, 0xCF };

        private static byte[] k1 = new byte[32]	{
            (byte) 131,
            (byte) 161,
            (byte) 25,
            (byte) 63,
            (byte) 78,
            (byte) 17,
            (byte) 203,
            (byte) 133,
            (byte) 155,
            (byte) 126,
            (byte) 31,
            (byte) 125,
            (byte) 249,
            (byte) 58,
            (byte) 64,
            (byte) 28,
            (byte) 235,
            (byte) 236,
            (byte) 88,
            (byte) 197,
            (byte) 21,
            (byte) 92,
            (byte) 67,
            (byte) 227,
            (byte) 167,
            (byte) 7,
            (byte) 22,
            (byte) 84,
            (byte) 122,
            (byte) 210,
            (byte) 213,
            (byte) 163
        };
        private static byte[] k2 = new byte[16] {
            (byte) 71,
            (byte) 137,
            (byte) 177,
            (byte) 73,
            (byte) 65,
            (byte) 71,
            (byte) 74,
            (byte) 5,
            (byte) 144,
            (byte) 212,
            (byte) 242,
            (byte) 157,
            (byte) 170,
            (byte) 239,
            (byte) 62,
            (byte) 160
        };

        public static EArchive Open(string path) {
            StringBuilder hex = new StringBuilder(k1.Length * 2);

            foreach (byte b in k1)
                hex.AppendFormat("{0:x2}", b);

            Debug.WriteLine(hex.ToString());

            hex = new StringBuilder(k2.Length * 2);

            foreach (byte b in k2)
                hex.AppendFormat("{0:x2}", b);

            Debug.WriteLine(hex.ToString());

            //* make sure an existing file was passed in
            if (!File.Exists(path))
                throw new FileNotFoundException();

            //* create the archive to be returned
            EArchive archive = null;

            //* open a stream to the file
            using (Stream stream = File.OpenRead(path)) {
                //* create a binary reader to read the file
                using (BinaryReader reader = new BinaryReader(stream, Encoding.Default)) {
                    //* pull the meta data of the archive
                    archive = ReadArchiveHeader(reader);

                    //* skip the padding
                    reader.ReadBytes(16);

                    //* set the path of the archive
                    archive.ArchivePath = path;

                    //* set the size of the archive
                    FileInfo info = new FileInfo(path);
                    archive.Size = info.Length;

                    //* create a list to store the files
                    archive.Files = new List<EArchiveFile>();

                    //* create the rolling key
                    long rollingKey = archive.MasterKey ^ archive.ArchiveKey;

                    //* read each file's header
                    for (int index = 0; index < archive.FileCount; index++) {
                        EArchiveFile file = ReadFileHeader(archive, reader, ref rollingKey);

                        //* add the file to the archive's list
                        archive.Files.Add(file);
                    }
                }
            }

            //* return our archive
            return archive;
        }

        private static EArchive ReadArchiveHeader(BinaryReader reader) {
            //* grab the file magic #
            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));

            //* if we got the wrong magic #, throw exception
            if (magic != "CRAF")
                throw new InvalidDataException("Expected CRAF, found " + magic);

            /*
            struct SQEX::Luminous::AssetManager::LmArcInterface::ArcHeader
            {
              unsigned int tag;
              unsigned __int16 minor;
              unsigned __int16 major;
              unsigned int count;
              unsigned int blockSize;
              unsigned int tocStart;
              unsigned int nameStart;
              unsigned int fullPathStart;
              unsigned int dataStart;
              unsigned int flags;
              unsigned int chunkSize;
              unsigned __int64 hash;
              char _pad[16];
            };

            enum SQEX::Luminous::AssetManager::LmArcInterface::ArcHeaderFlags
            {
              ARCHEADER_HASLOOSEDATA = 0x1,
              ARCHEADER_HASLOCALEDATA = 0x2,
              ARCHEADER_DEBUGARCHIVE = 0x4,
            };
            */

            //* read archive header and return archive
            return new EArchive {
                VersionMinor		= reader.ReadUInt16(),
                VersionMajor		= reader.ReadByte(),
                EncryptedMetaData	= (reader.ReadByte() == 0x80),
                FileCount			= reader.ReadInt32(),
                BlockSize			= reader.ReadUInt32(),

                //* 0x10
                FileHeaderOffset	= reader.ReadUInt32(),
                FileDataPathOffset	= reader.ReadUInt32(),
                FilePathOffset		= reader.ReadUInt32(),
                FileDataOffset		= reader.ReadUInt32(),

                //* 0x20
                Flags				= reader.ReadUInt32(),
                ChunkSize			= reader.ReadUInt32(),
                ArchiveKey			= reader.ReadInt64()
            };
        }

        private static EArchiveFile ReadFileHeader(EArchive archive, BinaryReader reader, ref long rollingKey) {
            /*
            struct SQEX::Luminous::AssetManager::LmArcInterface::ArcCatalogEntry
            {
              unsigned __int64 nameTypeHash;
              unsigned int originalSize;
              unsigned int compressedSize;
              unsigned int flags;
              unsigned int nameStart;
              unsigned __int64 dataStart;
              unsigned int fullPathStart;
              _BYTE localizeType[1];
              _BYTE localizeLocale[1];
              unsigned __int16 key;
            };

            enum SQEX::Luminous::AssetManager::LmArcInterface::ArcFlags
            {
                ARCFLAG_AUTOLOAD = 0x1,
                ARCFLAG_COMPRESSED = 0x2,
                ARCFLAG_REFERENCE = 0x4,
                ARCFLAG_NOEARC = 0x8,
                ARCFLAG_PATCHED = 0x10,
                ARCFLAG_PATCHED_DELETED = 0x20,
            };
            */
            //* read header data
            EArchiveFile file = new EArchiveFile {
                Hash				= reader.ReadInt64(),
                SizeUncompressed	= reader.ReadInt32(),
                SizeCompressed		= reader.ReadInt32(),
                Flags				= reader.ReadUInt32(),
                DataPathOffset		= reader.ReadUInt32(),
                DataOffset			= reader.ReadInt64(),
                PathOffset			= reader.ReadUInt32(),
                LocalizeType		= reader.ReadByte(),
                Locale				= reader.ReadByte(),
                ChunkKey			= reader.ReadUInt16()
            };

            //* store the current position as it points to the next file header
            long position = reader.BaseStream.Position;

            //* jump to the data path offset and store it
            reader.BaseStream.Seek(file.DataPathOffset, SeekOrigin.Begin);
            file.DataPath = reader.ReadCString();

            //* jump to the path offset and store it
            reader.BaseStream.Seek(file.PathOffset, SeekOrigin.Begin);
            file.Path = reader.ReadCString();

            //* if the archive encrypts the meta data, decrypt it
            //* thanks to daxxy
            if (archive.EncryptedMetaData && (file.Flags & 0x80) == 0) {
                long	fileSizeKey		= (rollingKey * MasterFileKey) ^ file.Hash,
                        dataOffsetKey	= (fileSizeKey * MasterFileKey) ^ ~(file.Hash);

                int		uncompressedKey = (int)(fileSizeKey >> 32),
                        compressedKey	= (int)(fileSizeKey & 0xFFFFFFFF);

                file.SizeUncompressed	^= uncompressedKey;
                file.SizeCompressed		^= compressedKey;
                file.DataOffset			^= dataOffsetKey;

                rollingKey				= dataOffsetKey;
            }

            //* if the file is encrypted, get the IV
            //* thanks to daxxy
            if (file.Encrypted) {
                reader.BaseStream.Seek(file.DataOffset + file.SizeCompressed - 0x21, SeekOrigin.Begin);
                file.IV = reader.ReadBytes(16);
            }

            //* store our archive path
            file.ArchivePath = archive.ArchivePath;
            //* store whether this header information is encrypted (keeps us from passing around the archive data)
            file.HeaderEncrypted = archive.EncryptedMetaData;

            //* get our file name
            file.Filename = file.Path.Substring(file.Path.LastIndexOf('/') + 1);
            //* get our directory
            file.Directory = file.Path.Substring(0, file.Path.LastIndexOf('/') + 1);

            //* jump to the next file header
            reader.BaseStream.Seek(position, SeekOrigin.Begin);

            return file;
        }

        public static void ExtractFile(EArchive archiveData, EArchiveFile file, string path) {
            //* make sure the path exists
            if (!Directory.Exists(path + file.Directory))
                Directory.CreateDirectory(path + file.Directory);

            //* open our archive
            using (Stream archive = File.OpenRead(file.ArchivePath)) {
                using (BinaryReader reader = new BinaryReader(archive)) {
                    //* move to our data
                    archive.Seek(file.DataOffset, SeekOrigin.Begin);

                    //* see if our file is compressed
                    if (file.Compressed)
                        ExtractCompressedFile(archiveData, file, reader, path);
                    //* see if our file is encrypted
                    else if (file.Encrypted)
                        ExtractEncryptedFile(file, reader, path);
                    //* extract a normal file
                    else
                        ExtractPlainFile(file, reader, path);
                }
            }
        }

        private static void ExtractCompressedFile(EArchive archive, EArchiveFile file, BinaryReader reader, string path) {
            //* get the number of chunks we have
            int chunkSize = (int)archive.ChunkSize * 1024;
            int chunks = file.SizeUncompressed / chunkSize;

            //* if the integer division wasn't even, add 1 more chunk
            if (file.SizeUncompressed % chunkSize != 0)
                chunks++;

            //try {
            using (Stream write = File.Create(path + file.Path)) {
                using (BinaryWriter writer = new BinaryWriter(write)) {
                    //* loop through each chunk writing data as we go
                    for (int index = 0; index < chunks; index++) {
                        //* align our bytes
                        if (index > 0) {
                            int offset = 4 - (int)(reader.BaseStream.Position % 4);

                            if (offset > 3)
                                offset = 0;

                            reader.BaseStream.Seek(offset, SeekOrigin.Current);
                        }

                        uint sizeCompressed		= reader.ReadUInt32(),
                            sizeUncompressed	= reader.ReadUInt32();

                        //* if our header is encrypted, decrypt the sizes
                        if (index == 0 && file.HeaderEncrypted) {
                            long chunkKey		= (MasterChunkKeyA * file.ChunkKey) + MasterChunkKeyB;

                            uint compressedKey	= (uint)(chunkKey >> 32);
                            uint uncompressedKey = (uint)(chunkKey & 0xFFFFFFFF);

                            sizeCompressed		^= compressedKey;
                            sizeUncompressed	^= uncompressedKey;
                        }

                        using (MemoryStream memory = new MemoryStream()) {
                            //* store the chunk of compressed data to a memory stream
                            memory.Write(reader.ReadBytes((int)sizeCompressed), 0, (int)sizeCompressed);

                            //* move to the start of the chunk
                            memory.Seek(0, SeekOrigin.Begin);

                            //* now decompress it and write it to our file
                            using (ZLibStream decompressor = new ZLibStream(memory, CompressionMode.Decompress)) {
                                for (int position = 0; position < sizeUncompressed; position++)
                                    writer.Write((byte)decompressor.ReadByte());
                            }
                        }
                    }
                }
            }
            //}
            //catch (Exception ex) {
            //	throw ex;
            //}
        }

        private static void ExtractEncryptedFile(EArchiveFile file, BinaryReader reader, string path) {
            Aes aes = Aes.Create();

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.Zeros;

            ICryptoTransform transform = aes.CreateDecryptor(AESKey, file.IV);

            using (CryptoStream stream = new CryptoStream(reader.BaseStream, transform, CryptoStreamMode.Read)) {
                using (Stream write = File.Create(path + file.Path)) {
                    using (BinaryWriter writer = new BinaryWriter(write)) {
                        for (int index = 0; index < file.SizeUncompressed; index++) {
                            writer.Write((byte)stream.ReadByte());
                        }
                    }
                }
            }
        }

        private static void ExtractPlainFile(EArchiveFile file, BinaryReader reader, string path) {
            using (Stream write = File.Create(path + file.Path)) {
                using (BinaryWriter writer = new BinaryWriter(write)) {
                    for (int index = 0; index < file.SizeUncompressed; index++)
                        writer.Write(reader.ReadByte());
                }
            }
        }

        public static void CreateArchive(string path, EArchive archive, List<EArchiveFileCreation> files) {
            byte zero = 0x00;

            if (archive.ArchiveKey == 0)
                archive.ArchiveKey = (long)((new Random().NextDouble() * 2.0 - 1.0) * long.MaxValue);

            using (Stream stream = File.Create(path)) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    //* write out the archive header
                    WriteArchiveHeader(writer, archive, files.Count);

                    //* zero fill header data to come back to later
                    for (int count = 0; count < files.Count * 40; count++)
                        writer.Write(zero);

                    //* pad 8 0's
                    PadZeroes(writer, 8);

                    //* write the data paths
                    for (int index = 0; index < files.Count; index++)
                        WriteFileDataPath(writer, files[index]);

                    //* pad 8 0's
                    PadZeroes(writer, 8);

                    //* write the paths
                    for (int index = 0; index < files.Count; index++) {
                        WriteFilePath(writer, files[index]);

                        //* if it's the first file, update the archive's header
                        if (index == 0) {
                            long current = stream.Position;

                            stream.Seek(24, SeekOrigin.Begin);
                            writer.Write(files[index].PathOffset);

                            stream.Seek(current, SeekOrigin.Begin);
                        }
                    }

                    //* pad 8 0's
                    PadZeroes(writer, 8);

                    //* align to 512
                    Align(writer, (int)archive.BlockSize);

                    //* write file data
                    for (int index = 0; index < files.Count; index++) {
                        if (files[index].Hash == 0) {
                            files[index].Hash = FNV1A64(files[index].Path.Substring(files[index].Path.LastIndexOf(".") + 1), MasterFileHash, true);
                            files[index].Hash = files[index].Hash | FNV1A64Lower(files[index].DataPath, MasterFileHash, true);
                        }

                        //* store our data offset
                        files[index].DataOffset = stream.Position;

                        //* if our filepath matches our path, it's data from an existing archive
                        if (files[index].FilePath == files[index].Path)
                            CopyFileFromArchive(writer, archive, files[index]);
                        //* is it suppose to be encrypted?
                        else if (files[index].Encrypted)
                            WriteEncryptedFile(writer, archive, files[index]);
                        //* if the file has no size, there's nothing to write but we still need to align
                        else if (files[index].SizeUncompressed == 0)
                            WriteZeroLengthFile(writer, archive);
                        //* if the file isn't compressed, write the uncompressed file
                        else if (!files[index].Compressed)
                            WriteUncompressedFile(writer, files[index]);
                        //* otherwise, compress it and write it
                        else
                            WriteCompressedFile(writer, archive, files[index]);

                        //* if it's the first file, update the archive's header
                        if (index == 0) {
                            long current = stream.Position;

                            stream.Seek(28, SeekOrigin.Begin);
                            writer.Write((int)files[index].DataOffset);

                            stream.Seek(current, SeekOrigin.Begin);
                        }

                        //* align to 512
                        Align(writer, (int)archive.BlockSize);
                    }

                    //* start file headers
                    long rollingKey = archive.MasterKey ^ archive.ArchiveKey;

                    stream.Seek(0x40, SeekOrigin.Begin);

                    for (int index = 0; index < files.Count; index++)
                        WriteFileHeader(writer, archive, files[index], ref rollingKey);
                }
            }
        }

        private static void WriteFileHeader(BinaryWriter writer, EArchive archive, EArchiveFileCreation file, ref long rollingKey) {
            long	fileSizeKey		= (rollingKey * MasterFileKey) ^ file.Hash,
                    dataOffsetKey	= (fileSizeKey * MasterFileKey) ^ ~(file.Hash);

            int		uncompressedKey = (int)(fileSizeKey >> 32),
                    compressedKey	= (int)(fileSizeKey & 0xFFFFFFFF);

            file.SizeUncompressed ^= uncompressedKey;
            file.SizeCompressed ^= compressedKey;
            file.DataOffset ^= dataOffsetKey;

            rollingKey = dataOffsetKey;

            writer.Write(file.Hash);
            writer.Write(file.SizeUncompressed);
            writer.Write(file.SizeCompressed);

            writer.Write(file.Flags);
            writer.Write(file.DataPathOffset);
            writer.Write(file.DataOffset);
            writer.Write(file.PathOffset);
            writer.Write(file.LocalizeType);
            writer.Write(file.Locale);
            writer.Write(file.ChunkKey);
        }

        private static void WriteEncryptedFile(BinaryWriter writer, EArchive archive, EArchiveFileCreation file) {
            Aes aes = Aes.Create();

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.Zeros;

            if (file.IV == null) {
                aes.GenerateIV();
                file.IV = aes.IV;
            }

            ICryptoTransform transform = aes.CreateEncryptor(AESKey, file.IV);

            using (Stream input = File.OpenRead(file.FilePath)) {
                using (BinaryReader inputReader = new BinaryReader(input)) {
                    using (MemoryStream msEncrypted = new MemoryStream()) {
                        using (CryptoStream csEncrypted = new CryptoStream(msEncrypted, transform, CryptoStreamMode.Write)) {
                            using (BinaryWriter bwEncrypted = new BinaryWriter(csEncrypted, Encoding.Default, true)) {
                                while (input.Position < input.Length)
                                    bwEncrypted.Write(inputReader.ReadByte());
                            }

                            if (!csEncrypted.HasFlushedFinalBlock)
                                csEncrypted.FlushFinalBlock();

                            msEncrypted.Seek(0, SeekOrigin.Begin);

                            while (msEncrypted.Position < msEncrypted.Length)
                                writer.Write((byte)msEncrypted.ReadByte());
                        }
                    }
                }
            }

            int alignment = 16 - (file.SizeUncompressedOriginal % 16);

            if (alignment == 16)
                alignment = 0;
            
            writer.Write(file.IV);
            file.SizeCompressed = file.SizeUncompressedOriginal + alignment + 33;
        }

        private static void CopyFileFromArchive(BinaryWriter writer, EArchive archive, EArchiveFileCreation file) {
            if (file.FilePath == file.Path) {
                EArchive existing = Open(file.ArchivePath);
                EArchiveFile old = null;

                foreach (EArchiveFile previous in existing.Files) {
                    if (previous.Path == file.Path) {
                        old = previous;
                        break;
                    }
                }

                if (old == null)
                    throw new FileNotFoundException();

                using (Stream input = File.OpenRead(file.ArchivePath)) {
                    input.Seek(old.DataOffset, SeekOrigin.Begin);

                    using (BinaryReader reader = new BinaryReader(input)) {
                        for (int index = 0; index < old.SizeCompressed; index++) {
                            writer.Write(reader.ReadByte());
                        }
                    }
                }

                file.SizeCompressed = old.SizeCompressed;
                file.SizeUncompressed = old.SizeUncompressed;
                file.SizeUncompressedOriginal = old.SizeUncompressed;

                return;
            }
        }

        private static void WriteCompressedFile(BinaryWriter writer, EArchive archive, EArchiveFileCreation file) {
            using (Stream input = File.OpenRead(file.FilePath)) {
                using (BinaryReader reader = new BinaryReader(input)) {
                    int chunkSize = (int)archive.ChunkSize * 1024;

                    file.SizeUncompressedOriginal = (int)input.Length;
                    file.SizeUncompressed = (int)input.Length;

                    //* get the number of chunks
                    int		chunks					= (int)input.Length / chunkSize,
                            remaining				= (int)input.Length;

                    //* if the integer division wasn't even, add a chunk
                    if (input.Length % chunkSize != 0)
                        chunks++;

                    //* set compressed size to zero and sum as we go
                    file.SizeCompressed = 0;

                    if (file.ChunkKey == 0)
                        file.ChunkKey = (ushort)(new Random().Next(0, ushort.MaxValue));

                    for (int chunk = 0; chunk < chunks; chunk++) {
                        //* use a memory stream for chunking
                        using (MemoryStream memory = new MemoryStream()) {
                            int read = (remaining > chunkSize) ? chunkSize : remaining;

                            //* store the chunk of compressed data to a memory stream
                            memory.Write(reader.ReadBytes(read), 0, read);

                            //* move to the start of the chunk
                            memory.Seek(0, SeekOrigin.Begin);

                            //* now compress it and write it to our compressed stream
                            using (MemoryStream compressed = new MemoryStream()) {
                                using (ZLibStream compressor = new ZLibStream(compressed, CompressionMode.Compress, CompressionLevel.Best, true))
                                    memory.CopyTo(compressor);

                                compressed.Seek(0, SeekOrigin.Begin);

                                int sizeCompressed = (int)compressed.Length,
                                    sizeUncompressed = read;

                                //* write chunk sizes
                                if (chunk == 0) {
                                    //* for the first one we need to encrypt it
                                    long chunkKey = (MasterChunkKeyA * file.ChunkKey) + MasterChunkKeyB;

                                    int compressedKey = (int)(chunkKey >> 32);
                                    int uncompressedKey = (int)(chunkKey & 0xFFFFFFFF);

                                    sizeCompressed ^= compressedKey;
                                    sizeUncompressed ^= uncompressedKey;
                                }

                                writer.Write(sizeCompressed);
                                writer.Write(sizeUncompressed);

                                for (int position = 0; position < compressed.Length; position++)
                                    writer.Write((byte)compressed.ReadByte());

                                //* align for next chunk
                                int alignment = Align(writer, 4);

                                file.SizeCompressed += (int)compressed.Length + 8 + alignment;
                            }

                            remaining -= read;
                        }
                    }
                }
            }
        }

        private static void WriteUncompressedFile(BinaryWriter writer, EArchiveFileCreation file) {
            //* create a stream to read the file contents
            using (Stream input = File.OpenRead(file.FilePath)) {
                using (BinaryReader reader = new BinaryReader(input)) {
                    while (reader.BaseStream.Position < reader.BaseStream.Length) {
                        writer.Write(reader.ReadByte());
                    }
                }
            }
        }

        private static void WriteZeroLengthFile(BinaryWriter writer, EArchive archive) {
            for (int count = 0; count < archive.BlockSize; count++)
                writer.Write((byte)0x00);
        }

        private static void WriteArchiveHeader(BinaryWriter writer, EArchive archive, int files) {
            writer.Write(Encoding.ASCII.GetBytes("CRAF"));
            writer.Write((ushort)archive.VersionMinor);
            writer.Write((byte)archive.VersionMajor);
            writer.Write((archive.EncryptedMetaData) ? (byte)0x80 : (byte)0x00);
            writer.Write(files);
            writer.Write(archive.BlockSize);
            writer.Write(0x40); //* should always be 0x40

            int dataPathOffset = files * 40 + 8 + 64;

            writer.Write(dataPathOffset);
            writer.Write(archive.FilePathOffset);
            writer.Write(archive.FileDataOffset);
            writer.Write(archive.Flags);
            writer.Write(archive.ChunkSize);
            writer.Write(archive.ArchiveKey);

            //* write padding
            writer.Write(archive.Padding);
        }

        private static void WriteFileDataPath(BinaryWriter writer, EArchiveFileCreation file) {
            //* store our data path offset
            file.DataPathOffset = (uint)writer.BaseStream.Position;

            //* write the data path
            if (string.IsNullOrWhiteSpace(file.DataPath))
                writer.Write(Encoding.UTF8.GetBytes("data://" + file.Path));
            else
                writer.Write(Encoding.UTF8.GetBytes(file.DataPath));

            //* zero terminate
            writer.Write((byte)0x00);

            //* align for next one
            Align(writer, 8);
        }

        private static void WriteFilePath(BinaryWriter writer, EArchiveFileCreation file) {
            //* store the path offset
            file.PathOffset = (uint)writer.BaseStream.Position;

            //* write the data path
            writer.Write(Encoding.UTF8.GetBytes(file.Path));

            //* zero terminate
            writer.Write((byte)0x00);

            //* align for next one
            Align(writer, 8);
        }

        private static void PadZeroes(BinaryWriter writer, int amount) {
            //* pad 8 0's
            for (int index = 0; index < amount; index++)
                writer.Write((byte)0x00);
        }

        private static int Align(BinaryWriter writer, int to) {
            int count = 0;

            while (writer.BaseStream.Position % to != 0) {
                writer.Write((byte)0x00);
                count++;
            }

            return count;
        }

        private static long FNV1A64(string text, long key, bool shift) {
            byte[] data = Encoding.UTF8.GetBytes(text);

            for (int index = 0; index < data.Length; index++) {
                byte character = data[index];

                key = key ^ character;

                key = MasterFileKey * key;
            }

            if (shift)
                key = key << 44;

            return key;
        }

        private static long FNV1A64Lower(string text, long key, bool trunc) {
            byte[] data = Encoding.UTF8.GetBytes(text);
            long hash = key;

            for (int index = 0; index < data.Length; index++) {
                byte character = data[index],
                     check = character;

                int difference = character - 65;

                if (difference < 0)
                    check = (byte)(byte.MaxValue + difference);
                else
                    check = (byte)(character - 65);

                if (check <= 25)
                    character += 32;

                hash = MasterFileKey * (long)((character ^ (ulong)hash));
            }

            if (trunc) {
                long test = 0x0FFFFFFFFFFF;

                hash = hash & test;
            }

            return hash;
        }
    }
}
