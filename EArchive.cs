using System.Collections.Generic;

namespace earc2 {
    public class EArchive {
        /// <summary>
        /// Gets the full version of the archive.
        /// </summary>
        public string Version {
            get => this.p_VersionMajor + "." + this.p_VersionMinor;
        }

        /// <summary>
        /// Gets or sets the version major of the archive.
        /// </summary>
        public int VersionMajor {
            get => this.p_VersionMajor;
            set => this.p_VersionMajor = value;
        }

        /// <summary>
        /// Gets or sets the version minor of the archive.
        /// </summary>
        public int VersionMinor {
            get => this.p_VersionMinor;
            set => this.p_VersionMinor = value;
        }

        private int p_VersionMajor = 0,
                    p_VersionMinor = 0;

        /// <summary>
        /// Gets or sets the size, in bytes, of the archive.
        /// </summary>
        public long Size {
            get => this.p_Size;
            set => this.p_Size = value;
        }

        private long p_Size = 0;

        /// <summary>
        /// Gets or sets whether the archive has encrypted meta data.
        /// </summary>
        public bool EncryptedMetaData {
            get => this.p_EncryptedMetaData;
            set => this.p_EncryptedMetaData = value;
        }

        private bool p_EncryptedMetaData = false;

        /// <summary>
        /// Gets or sets the path of the archive.
        /// </summary>
        public string ArchivePath {
            get => this.p_ArchivePath;
            set => this.p_ArchivePath = value;
        }

        private string p_ArchivePath = string.Empty;

        /// <summary>
        /// Gets the encryption key to use with this archive.
        /// </summary>
        public long MasterKey {
            get => (this.Flags & 8) != 0 ? EARC.MasterArchiveKeyB : EARC.MasterArchiveKeyA;
        }

        /// <summary>
        /// Gets or sets the number of files in this archive.
        /// </summary>
        public int FileCount {
            get => this.p_FileCount;
            set => this.p_FileCount = value;
        }

        private int p_FileCount = 0;

        /// <summary>
        /// Gets or sets the offset of the first file header in this archive.
        /// </summary>
        public uint FileHeaderOffset {
            get => this.p_FileHeaderOffset;
            set => this.p_FileHeaderOffset = value;
        }

        private uint p_FileHeaderOffset = 0;

        /// <summary>
        /// Gets or sets the offset of the first file data path in this archive.
        /// </summary>
        public uint FileDataPathOffset {
            get => this.p_FileDataPathOffset;
            set => this.p_FileDataPathOffset = value;
        }

        private uint p_FileDataPathOffset = 0;

        /// <summary>
        /// Gets or sets the offset of the first file path in this archive.
        /// </summary>
        public uint FilePathOffset {
            get => this.p_FilePathOffset;
            set => this.p_FilePathOffset = value;
        }

        private uint p_FilePathOffset = 0;

        /// <summary>
        /// Gets or sets the offset of the first file's data in this archive.
        /// </summary>
        public uint FileDataOffset {
            get => this.p_FileDataOffset;
            set => this.p_FileDataOffset = value;
        }

        private uint p_FileDataOffset = 0;

        /// <summary>
        /// Gets or sets the flags of the archive.
        /// </summary>
        public uint Flags {
            get => this.p_Flags;
            set => this.p_Flags = value;
        }

        private uint p_Flags = 0;

        /// <summary>
        /// Gets or sets the archive encryption key.
        /// </summary>
        public long ArchiveKey {
            get => this.p_ArchiveKey;
            set => this.p_ArchiveKey = value;
        }

        private long p_ArchiveKey = 0;

        public uint BlockSize {
            get => this.p_BlockSize;
            set => this.p_BlockSize = value;
        }

        private uint p_BlockSize = 0;

        public uint ChunkSize {
            get => this.p_ChunkSize;
            set => this.p_ChunkSize = value;
        }

        private uint p_ChunkSize = 0;

        public byte[] Padding {
            get => new byte[16] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        }

        /// <summary>
        /// Gets or sets the files of the archive.
        /// </summary>
        public List<EArchiveFile> Files {
            get => this.p_Files;
            set => this.p_Files = value;
        }

        private List<EArchiveFile> p_Files = null;
    }

    public class EArchiveFile {
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

        /// <summary>
        /// Gets or sets the uncompressed size of this file.
        /// </summary>
        public int SizeUncompressed {
            get => this.p_SizeUncompressed;
            set => this.p_SizeUncompressed = value;
        }

        private int p_SizeUncompressed = 0;

        /// <summary>
        /// Gets or sets the compressed size of this file.
        /// </summary>
        public int SizeCompressed {
            get => this.p_SizeCompressed;
            set => this.p_SizeCompressed = value;
        }

        private int p_SizeCompressed = 0;

        /// <summary>
        /// Gets or sets the flags for this file.
        /// </summary>
        public uint Flags {
            get => this.p_Flags;
            set => this.p_Flags = value;
        }

        private uint p_Flags = 0;

        /// <summary>
        /// Gets whether this file is compressed.
        /// </summary>
        public bool Compressed {
            get => (this.p_Flags & 2) != 0;
        }

        /// <summary>
        /// Gets whether this file is encrypted.
        /// </summary>
        public bool Encrypted {
            get => (this.p_Flags & 0x40) != 0;
        }

        /// <summary>
        /// Gets or sets the directory of this file. This is the path of the file without the file name or extension.
        /// </summary>
        public string Directory {
            get => this.p_Directory;
            set => this.p_Directory = value;
        }

        private string p_Directory = string.Empty;

        /// <summary>
        /// Gets or sets the filename of this file. This is the path of the file with only the file name and extension.
        /// </summary>
        public string Filename {
            get => this.p_Filename;
            set => this.p_Filename = value;
        }

        private string p_Filename = string.Empty;

        /// <summary>
        /// Gets or sets the path of this file.
        /// </summary>
        public string Path {
            get => this.p_Path;
            set => this.p_Path = value;
        }

        private string p_Path = string.Empty;

        /// <summary>
        /// Gets or sets the data path of this file.
        /// </summary>
        public string DataPath {
            get => this.p_DataPath;
            set => this.p_DataPath = value;
        }

        private string p_DataPath = string.Empty;

        /// <summary>
        /// Gets or sets the IV for the AES encryption of this file.
        /// </summary>
        public byte[] IV {
            get => this.p_IV;
            set => this.p_IV = value;
        }

        private byte[] p_IV = null;

        /// <summary>
        /// Gets or sets the file hash.
        /// </summary>
        public long Hash {
            get => this.p_Hash;
            set => this.p_Hash = value;
        }

        private long p_Hash = 0;

        /// <summary>
        /// Gets or sets the chunk encryption key if the file is compressed.
        /// </summary>
        public ushort ChunkKey {
            get => this.p_ChunkKey;
            set => this.p_ChunkKey = value;
        }

        private ushort p_ChunkKey = 0;

        /// <summary>
        /// Gets or sets the offset of the data path for this file.
        /// </summary>
        public uint DataPathOffset {
            get => this.p_DataPathOffset;
            set => this.p_DataPathOffset = value;
        }

        private uint p_DataPathOffset = 0;

        /// <summary>
        /// Gets or sets the offset of the path for this file.
        /// </summary>
        public uint PathOffset {
            get => this.p_PathOffset;
            set => this.p_PathOffset = value;
        }

        private uint p_PathOffset = 0;

        /// <summary>
        /// Gets or sets the offset of the data for this file.
        /// </summary>
        public long DataOffset {
            get => this.p_DataOffset;
            set => this.p_DataOffset = value;
        }

        private long p_DataOffset = 0;

        /// <summary>
        /// Gets or sets the path to the archive containing this file.
        /// </summary>
        public string ArchivePath {
            get => this.p_ArchivePath;
            set => this.p_ArchivePath = value;
        }

        private string p_ArchivePath = string.Empty;

        /// <summary>
        /// Gets or sets whether the header of this file is encrypted.
        /// </summary>
        public bool HeaderEncrypted {
            get => this.p_HeaderEncrypted;
            set => this.p_HeaderEncrypted = value;
        }

        private bool p_HeaderEncrypted = false;

        public byte LocalizeType {
            get => this.p_LocalizeType;
            set => this.p_LocalizeType = value;
        }

        private byte p_LocalizeType = 0;

        public byte Locale {
            get => this.p_Locale;
            set => this.p_Locale = value;
        }

        private byte p_Locale = 0;
    }

    public class EArchiveFileCreation : EArchiveFile {
        /// <summary>
        /// Gets or sets the location of the actual file on the hard disk.
        /// </summary>
        public string FilePath {
            get {
                if (string.IsNullOrWhiteSpace(this.p_FilePath))
                    return this.Path;

                return this.p_FilePath;
            }

            set => this.p_FilePath = value;
        }

        private string p_FilePath = string.Empty;

        public int SizeUncompressedOriginal {
            get => this.p_SizeUncompressedOriginal;
            set => this.p_SizeUncompressedOriginal = value;
        }

        private int p_SizeUncompressedOriginal = 0;
    }
}
