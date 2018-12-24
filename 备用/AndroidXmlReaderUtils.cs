using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml;

namespace AndroidXml.Res
{
    #region Res_value
    [Serializable]
    public class Res_value
    {
        /// <summary>
        /// Number of bytes in this structure. Always 8.
        /// </summary>
        public ushort Size { get; set; }

        /// <summary>
        /// Reserved. Always 0.
        /// </summary>
        public byte Res0 { get; set; }

        /// <summary>
        /// The type of the data.
        /// </summary>
        public ValueType DataType { get; set; }

        /// <summary>
        /// The raw value of the data.
        /// </summary>
        public uint RawData { get; set; }

        /// <summary>
        /// Gets or sets the data as a resource reference. Used when <see cref="DataType"/> is <see cref="ValueType.TYPE_REFERENCE"/>.
        /// </summary>
        /// <remarks>
        /// Assignments to fields of the <c>ResTable_ref</c> object will 
        /// not be detected. You have to reassign <c>ReferenceValue</c>
        /// upon change.
        /// </remarks>
        public ResTable_ref ReferenceValue
        {
            get
            {
                return new ResTable_ref
                {
                    Ident = RawData == 0xFFFFFFFFu ? (uint?) null : RawData
                };
            }
            set { RawData = value.Ident ?? 0xFFFFFFFFu; }
        }

        /// <summary>
        /// Gets or sets the data as a string reference. Used when <see cref="DataType"/> is <see cref="ValueType.TYPE_STRING"/>.
        /// </summary>
        /// <remarks>
        /// Assignments to fields of the <c>ResStringPool_ref</c> object will 
        /// not be detected. You have to reassign <c>StringValue</c>
        /// upon change.
        /// </remarks>
        public ResStringPool_ref StringValue
        {
            get
            {
                return new ResStringPool_ref
                {
                    Index = RawData == 0xFFFFFFFFu ? (uint?) null : RawData
                };
            }
            set { RawData = value.Index ?? 0xFFFFFFFFu; }
        }

        /// <summary>
        /// Gets or sets the data as a floating point value. Used when <see cref="DataType"/> is <see cref="ValueType.TYPE_FLOAT"/>.
        /// </summary>
        public float FloatValue
        {
            get { return BitConverter.ToSingle(BitConverter.GetBytes(RawData), 0); }
            set { RawData = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0); }
        }

        /// <summary>
        /// Gets or sets the data as a signed integer value. Used when <see cref="DataType"/> is <see cref="ValueType.TYPE_INT_DEC"/>  
        /// or <see cref="ValueType.TYPE_INT_HEX"/>.
        /// </summary>
        public int IntValue
        {
            get { return (int) RawData; }
            set { RawData = (uint) value; }
        }

        /// <summary>
        /// Gets or sets the data as a color value. Used when <see cref="DataType"/> is <see cref="ValueType.TYPE_INT_COLOR_ARGB4"/>, 
        /// <see cref="ValueType.TYPE_INT_COLOR_ARGB8"/>, <see cref="ValueType.TYPE_INT_COLOR_RGB4"/> or 
        /// <see cref="ValueType.TYPE_INT_COLOR_RGB8"/>.
        /// </summary>
        public Color ColorValue
        {
            get
            {
                byte[] bytes = BitConverter.GetBytes(RawData);
                return new Color {A = bytes[3], R = bytes[2], G = bytes[1], B = bytes[0]};
            }
            set { RawData = BitConverter.ToUInt32(new[] {value.B, value.G, value.R, value.A}, 0); }
        }

        /// <summary>
        /// Gets or sets the unit of the data as a dimension value. Used when <see cref="DataType"/> is <see cref="ValueType.TYPE_DIMENSION"/>.
        /// </summary>
        public DimensionUnit ComplexDimensionUnit
        {
            get { return (DimensionUnit) (RawData & 0xFu); }
            set { RawData = (RawData & ~0xFu) | ((uint) value & 0xFu); }
        }

        /// <summary>
        /// Gets or sets the unit of the data as a fraction value. Used when <see cref="DataType"/> is <see cref="ValueType.TYPE_FRACTION"/>.
        /// </summary>
        public FractionUnit ComplexFractionUnit
        {
            get { return (FractionUnit) (RawData & 0xFu); }
            set { RawData = (RawData & ~0xFu) | ((uint) value & 0xFu); }
        }

        /// <summary>
        /// Gets or sets the number of the data as a complex value. Used when <see cref="DataType"/> is <see cref="ValueType.TYPE_DIMENSION"/> 
        /// or <see cref="ValueType.TYPE_FRACTION"/>.
        /// </summary>
        public float ComplexValue
        {
            get
            {
                uint radix = (RawData & 0x30u) >> 4;
                int mantissa = ((int) RawData & ~0xFF) >> 8; // MSB -> sign
                switch (radix)
                {
                    case 0: // 23p0
                        return mantissa;
                    case 1: // 16p7
                        return mantissa/128f;
                    case 2: // 8p15
                        return mantissa/32768f;
                    case 3: // 0p23
                    default:
                        return mantissa/8388608f;
                }
            }
            set
            {
                float abs = value < 0 ? -value : value;
                int sign = value < 0 ? -1 : 1;
                uint radix;
                int mantissa;
                if (abs < 1f)
                {
                    radix = 3; // 0p23
                    mantissa = (int) (abs*8388608f + 0.5f);
                }
                else if (abs < 256f)
                {
                    radix = 2; // 8p15
                    mantissa = (int) (abs*32768f + 0.5f);
                }
                else if (abs < 65536f)
                {
                    radix = 2; // 16p7
                    mantissa = (int) (abs*128f + 0.5f);
                }
                else if (abs < 8388608f)
                {
                    radix = 1; // 23p0
                    mantissa = (int) (abs + 0.5f);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", "Too large to store in a complex field");
                }
                mantissa *= sign;
                RawData = ((uint) (mantissa << 8)) | (radix << 4) | (RawData & 0xFu);
            }
        }
    }

    public enum DimensionUnit
    {
        /// Pixels (px)
        PX = 0,

        /// Device Independant Points (dip)
        DIP = 1,

        /// Scaled device independant Points (sp)
        SP = 2,

        /// points (pt)
        PT = 3,

        /// inches (in)
        IN = 4,

        /// millimeters (mm)
        MM = 5,
    }

    public enum FractionUnit
    {
        /// Fraction (%)
        FRACTION = 0,

        /// Fraction of parent (%p)
        FRACTION_PARENT = 1,
    }

    public enum ValueType
    {
        /// Contains no data.
        TYPE_NULL = 0x00,

        /// Resource reference as a <see cref="ResTable_ref"/>.
        TYPE_REFERENCE = 0x01,

        /// Attribute resource identifier (Not supported)
        TYPE_ATTRIBUTE = 0x02,

        /// String reference as a <see cref="ResStringPool_ref"/>.
        TYPE_STRING = 0x03,

        /// Float value.
        TYPE_FLOAT = 0x04,

        /// Complex dimension value. Float value + <see cref="DimensionUnit"/>.
        TYPE_DIMENSION = 0x05,

        /// Complex fraction value. Float value + <see cref="FractionUnit"/>.
        TYPE_FRACTION = 0x06,
        //TYPE_FIRST_INT = 0x10,
        /// Integer rendered in decimal.
        TYPE_INT_DEC = 0x10,

        /// Integer rendered in hexadecimal.
        TYPE_INT_HEX = 0x11,

        /// Integer rendered as a boolean.
        TYPE_INT_BOOLEAN = 0x12,
        //TYPE_FIRST_COLOR_INT = 0x1c,
        /// <see cref="Color"/> value rendered as #AARRGGBB
        TYPE_INT_COLOR_ARGB8 = 0x1c,

        /// <see cref="Color"/> value rendered as #RRGGBB (alpha = FF)
        TYPE_INT_COLOR_RGB8 = 0x1d,

        /// <see cref="Color"/> value rendered as #ARGB
        TYPE_INT_COLOR_ARGB4 = 0x1e,

        /// <see cref="Color"/> value rendered as #RGB
        TYPE_INT_COLOR_RGB4 = 0x1f,
        //TYPE_LAST_COLOR_INT = 0x1f,
        //TYPE_LAST_INT = 0x1f
    }
    #endregion

    #region ResChunk_header
[Serializable]
    public class ResChunk_header
    {
        /// Type identifier for this chunk.  The meaning of this value depends on the containing chunk.
        public ResourceType Type { get; set; }

        /// Size of the chunk header (in bytes).  Adding this value to the address of the chunk allows
        /// you to find its associated data (if any).
        public ushort HeaderSize { get; set; }

        /// Total size of this chunk (in bytes).  This is the chunkSize plus the size of any data 
        /// associated with the chunk.  Adding this value to the chunk allows you to completely skip
        /// its contents (including any child chunks).  If this value is the same as chunkSize, there 
        /// is no data associated with the chunk.
        public uint Size { get; set; }
    }

    public enum ResourceType
    {
        RES_NULL_TYPE = 0x0000,
        RES_STRING_POOL_TYPE = 0x0001,
        RES_TABLE_TYPE = 0x0002,
        RES_XML_TYPE = 0x0003,
        //RES_XML_FIRST_CHUNK_TYPE = 0x0100,
        RES_XML_START_NAMESPACE_TYPE = 0x0100,
        RES_XML_END_NAMESPACE_TYPE = 0x0101,
        RES_XML_START_ELEMENT_TYPE = 0x0102,
        RES_XML_END_ELEMENT_TYPE = 0x0103,
        RES_XML_CDATA_TYPE = 0x0104,
        //RES_XML_LAST_CHUNK_TYPE = 0x017f,
        RES_XML_RESOURCE_MAP_TYPE = 0x0180,
        RES_TABLE_PACKAGE_TYPE = 0x0200,
        RES_TABLE_TYPE_TYPE = 0x0201,
        RES_TABLE_TYPE_SPEC_TYPE = 0x0202
    };
    #endregion

    #region ResResourceMap
    public class ResResourceMap
    {
        public ResChunk_header Header { get; set; }
        public List<uint> ResouceIds { get; set; }

        public string GetResouceName(uint? resourceId, ResStringPool strings)
        {
            if (resourceId == null) return null;
            uint index = 0;
            foreach (uint id in ResouceIds)
            {
                if (id == resourceId)
                {
                    return strings.GetString(index);
                }
                index++;
            }
            return null;
        }
    }
    #endregion

    #region ResStringPool
    public class ResStringPool
    {
        public ResStringPool_header Header { get; set; }
        //public List<uint> StringIndices { get; set; }
        //public List<uint> StyleIndices { get; set; }
        public List<string> StringData { get; set; }
        public List<ResStringPool_span> StyleData { get; set; }

        public string GetString(ResStringPool_ref reference)
        {
            return GetString(reference.Index);
        }

        public string GetString(uint? index)
        {
            if (index == null) return "";
            if (index >= StringData.Count)
            {
                throw new ArgumentOutOfRangeException("index", index, string.Format("index >= {0}", StringData.Count));
            }
            return StringData[(int) index];
        }

        public uint? IndexOfString(string target)
        {
            if (string.IsNullOrEmpty(target)) return null;
            uint index = 0;
            foreach (string s in StringData)
            {
                if (s == target) return index;
                index++;
            }
            return null;
        }

        public IEnumerable<ResStringPool_span> GetStyles(uint stringIndex)
        {
            if (stringIndex >= StringData.Count)
            {
                throw new ArgumentOutOfRangeException(
                    "stringIndex", stringIndex, string.Format("index >= {0}", StringData.Count));
            }
            int currentIndex = 0;
            foreach (ResStringPool_span style in StyleData)
            {
                if (style.IsEnd)
                {
                    currentIndex++;
                    if (currentIndex > stringIndex)
                    {
                        break;
                    }
                }
                else if (currentIndex == stringIndex)
                {
                    yield return style;
                }
            }
        }
    }
    #endregion

    #region ResStringPool_header
    [Serializable]
    public class ResStringPool_header
    {
        public ResChunk_header Header { get; set; }
        public uint StringCount { get; set; }
        public uint StyleCount { get; set; }
        public StringPoolFlags Flags { get; set; }
        public uint StringStart { get; set; }
        public uint StylesStart { get; set; }
    }

    [Flags]
    public enum StringPoolFlags
    {
        SORTED_FLAG = 1 << 0,
        UTF8_FLAG = 1 << 8
    }
    #endregion

    #region ResStringPool_ref
    [Serializable]
    public class ResStringPool_ref
    {
        public uint? Index { get; set; }
    }
    #endregion

    #region ResStringPool_span
    [Serializable]
    public class ResStringPool_span
    {
        public ResStringPool_ref Name { get; set; }
        public uint FirstChar { get; set; }
        public uint LastChar { get; set; }

        public bool IsEnd
        {
            get { return Name.Index == null; }
            set
            {
                if (value)
                {
                    Name.Index = null;
                }
                else if (IsEnd)
                {
                    Name.Index = 0;
                }
            }
        }
    }
    #endregion

    #region ResTable_config
    [Serializable]
    public class ResTable_config
    {
        // Original properties
        public uint Size { get; set; }
        public uint IMSI { get; set; }
        public uint Locale { get; set; }
        public uint ScreenType { get; set; }
        public uint Input { get; set; }
        public uint ScreenSize { get; set; }
        public uint Version { get; set; }
        public uint ScreenConfig { get; set; }
        public uint ScreenSizeDp { get; set; }

        #region Derived properties

        #region IMSI derived properties

        /// Mobile country code (from SIM). 0 means "any"
        public ushort IMSI_MCC
        {
            get { return (ushort) Helper.GetBits(IMSI, 0xFFFFu, 16); }
            set { IMSI = Helper.SetBits(IMSI, value, 0xFFFFu, 16); }
        }

        /// Mobile network code (from SIM). 0 means "any"
        public ushort IMSI_MNC
        {
            get { return (ushort) Helper.GetBits(IMSI, 0xFFFFu, 0); }
            set { IMSI = Helper.SetBits(IMSI, value, 0xFFFFu, 0); }
        }

        #endregion

        #region Locale derived properties

        public string LocaleLanguage
        {
            get
            {
                byte[] bytes = BitConverter.GetBytes(Locale);
                return new string(new[] {(char) bytes[0], (char) bytes[1]});
            }
            set
            {
                if (value.Length != 2) throw new ArgumentException();
                byte[] bytes = BitConverter.GetBytes(Locale);
                bytes[0] = (byte) value[0];
                bytes[1] = (byte) value[1];
                Locale = BitConverter.ToUInt32(bytes, 0);
            }
        }

        public string LocaleCountry
        {
            get
            {
                byte[] bytes = BitConverter.GetBytes(Locale);
                return new string(new[] {(char) bytes[2], (char) bytes[3]});
            }
            set
            {
                if (value.Length != 2) throw new ArgumentException();
                byte[] bytes = BitConverter.GetBytes(Locale);
                bytes[2] = (byte) value[0];
                bytes[3] = (byte) value[1];
                Locale = BitConverter.ToUInt32(bytes, 0);
            }
        }

        #endregion

        #region ScreenType derived properties

        public ConfigOrientation ScreenTypeOrientation
        {
            get { return (ConfigOrientation) Helper.GetBits(ScreenType, 0xFFu, 24); }
            set { ScreenType = Helper.SetBits(ScreenType, (uint) value, 0xFFu, 24); }
        }

        public ConfigTouchscreen ScreenTypeTouchscreen
        {
            get { return (ConfigTouchscreen) Helper.GetBits(ScreenType, 0xFFu, 16); }
            set { ScreenType = Helper.SetBits(ScreenType, (uint) value, 0xFFu, 16); }
        }

        public ConfigDensity ScreenTypeDensity
        {
            get { return (ConfigDensity) Helper.GetBits(ScreenType, 0xFFFFu, 0); }
            set { ScreenType = Helper.SetBits(ScreenType, (uint) value, 0xFFFFu, 0); }
        }

        #endregion

        #region Input derived properties

        public ConfigKeyboard InputKeyboard
        {
            get { return (ConfigKeyboard) Helper.GetBits(Input, 0xFF, 24); }
            set { Input = Helper.SetBits(Input, (uint) value, 0xFF, 24); }
        }

        public ConfigNavigation InputNavigation
        {
            get { return (ConfigNavigation) Helper.GetBits(Input, 0xFF, 16); }
            set { Input = Helper.SetBits(Input, (uint) value, 0xFF, 16); }
        }

        public ConfigKeysHidden InputKeysHidden
        {
            get { return (ConfigKeysHidden) Helper.GetBits(Input, 0x3u, 8); }
            set { Input = Helper.SetBits(Input, (uint) value, 0x3u, 8); }
        }

        public ConfigNavHidden InputNavHidden
        {
            get { return (ConfigNavHidden) Helper.GetBits(Input, 0x3u, 10); }
            set { Input = Helper.SetBits(Input, (uint) value, 0x3u, 10); }
        }

        #endregion

        #region ScreenSize derived properties

        public ushort ScreenSizeWidth
        {
            get { return (ushort) Helper.GetBits(ScreenSize, 0xFFFFu, 16); }
            set { ScreenSize = Helper.SetBits(ScreenSize, value, 0xFFFFu, 16); }
        }

        public ushort ScreenSizeHeight
        {
            get { return (ushort) Helper.GetBits(ScreenSize, 0xFFFFu, 0); }
            set { ScreenSize = Helper.SetBits(ScreenSize, value, 0xFFFFu, 0); }
        }

        #endregion

        #region Version derived properties

        public ushort VersionSDK
        {
            get { return (ushort) Helper.GetBits(Version, 0xFFFFu, 16); }
            set { Version = Helper.SetBits(Version, value, 0xFFFFu, 16); }
        }

        public ushort VersionMinor
        {
            get { return (ushort) Helper.GetBits(Version, 0xFFFFu, 0); }
            set { Version = Helper.SetBits(Version, value, 0xFFFFu, 0); }
        }

        #endregion

        #region ScreenConfig derived properties

        public ConfigScreenSize ScreenConfigScreenSize
        {
            get { return (ConfigScreenSize) Helper.GetBits(ScreenConfig, 0xFu, 24); }
            set { ScreenConfig = Helper.SetBits(ScreenConfig, (uint) value, 0xFu, 24); }
        }

        public ConfigScreenLong ScreenConfigScreenLong
        {
            get { return (ConfigScreenLong) Helper.GetBits(ScreenConfig, 0x3u, 28); }
            set { ScreenConfig = Helper.SetBits(ScreenConfig, (uint) value, 0x3u, 28); }
        }

        public ConfigUIModeType ScreenConfigUIModeType
        {
            get { return (ConfigUIModeType) Helper.GetBits(ScreenConfig, 0xFu, 16); }
            set { ScreenConfig = Helper.SetBits(ScreenConfig, (uint) value, 0xFu, 16); }
        }

        public ConfigUIModeNight ScreenConfigUIModeNight
        {
            get { return (ConfigUIModeNight) Helper.GetBits(ScreenConfig, 0x3u, 20); }
            set { ScreenConfig = Helper.SetBits(ScreenConfig, (uint) value, 0x3u, 20); }
        }

        public ushort ScreenConfigSmallestScreenWidthDp
        {
            get { return (ushort) Helper.GetBits(ScreenConfig, 0xFFFFu, 0); }
            set { ScreenConfig = Helper.SetBits(ScreenConfig, value, 0xFFFFu, 0); }
        }

        #endregion

        #region ScreenSizeDp derived properties

        public ushort ScreenSizeDpWidth
        {
            get { return (ushort) Helper.GetBits(ScreenSizeDp, 0xFFFFu, 16); }
            set { ScreenSizeDp = Helper.SetBits(ScreenSizeDp, value, 0xFFFFu, 16); }
        }

        public ushort ScreenSizeDpHeight
        {
            get { return (ushort) Helper.GetBits(ScreenSizeDp, 0xFFFFu, 0); }
            set { ScreenSizeDp = Helper.SetBits(ScreenSizeDp, value, 0xFFFFu, 0); }
        }

        #endregion

        #endregion
    }

    #region Enums

    public enum ConfigOrientation
    {
        ORIENTATION_ANY = 0x0000,
        ORIENTATION_PORT = 0x0001,
        ORIENTATION_LAND = 0x0002,
        ORIENTATION_SQUARE = 0x0003,
    }

    public enum ConfigTouchscreen
    {
        TOUCHSCREEN_ANY = 0x0000,
        TOUCHSCREEN_NOTOUCH = 0x0001,
        TOUCHSCREEN_STYLUS = 0x0002,
        TOUCHSCREEN_FINGER = 0x0003,
    }

    public enum ConfigDensity
    {
        DENSITY_DEFAULT = 0,
        DENSITY_LOW = 120,
        DENSITY_MEDIUM = 160,
        DENSITY_TV = 213,
        DENSITY_HIGH = 240,
        DENSITY_NONE = 0xffff,
    }

    public enum ConfigKeyboard
    {
        KEYBOARD_ANY = 0x0000,
        KEYBOARD_NOKEYS = 0x0001,
        KEYBOARD_QWERTY = 0x0002,
        KEYBOARD_12KEY = 0x0003,
    }

    public enum ConfigNavigation
    {
        NAVIGATION_ANY = 0x0000,
        NAVIGATION_NONAV = 0x0001,
        NAVIGATION_DPAD = 0x0002,
        NAVIGATION_TRACKBALL = 0x0003,
        NAVIGATION_WHEEL = 0x0004,
    }

    public enum ConfigKeysHidden
    {
        KEYSHIDDEN_ANY = 0x0000,
        KEYSHIDDEN_NO = 0x0001,
        KEYSHIDDEN_YES = 0x0002,
        KEYSHIDDEN_SOFT = 0x0003,
    }

    public enum ConfigNavHidden
    {
        NAVHIDDEN_ANY = 0x0000,
        NAVHIDDEN_NO = 0x0001,
        NAVHIDDEN_YES = 0x0002,
    }

    public enum ConfigScreenSize
    {
        SCREENSIZE_ANY = 0x00,
        SCREENSIZE_SMALL = 0x01,
        SCREENSIZE_NORMAL = 0x02,
        SCREENSIZE_LARGE = 0x03,
        SCREENSIZE_XLARGE = 0x04,
    }

    public enum ConfigScreenLong
    {
        SCREENLONG_ANY = 0x00,
        SCREENLONG_NO = 0x01,
        SCREENLONG_YES = 0x02,
    }

    public enum ConfigUIModeType
    {
        UI_MODE_TYPE_ANY = 0x00,
        UI_MODE_TYPE_NORMAL = 0x01,
        UI_MODE_TYPE_DESK = 0x02,
        UI_MODE_TYPE_CAR = 0x03,
        UI_MODE_TYPE_TELEVISION = 0x04,
    }

    public enum ConfigUIModeNight
    {
        UI_MODE_NIGHT_ANY = 0x00,
        UI_MODE_NIGHT_NO = 0x01,
        UI_MODE_NIGHT_YES = 0x02,
    }

    #endregion
    #endregion

    #region ResTable_entry
    [Serializable]
    public class ResTable_entry
    {
        public ushort Size { get; set; }
        public EntryFlags Flags { get; set; }
        public ResStringPool_ref Key { get; set; }
    }

    [Flags]
    public enum EntryFlags
    {
        FLAG_COMPLEX = 0x0001,
        FLAG_PUBLIC = 0x0002,
    }
    #endregion

    #region ResTable_header
    [Serializable]
    public class ResTable_header
    {
        public ResChunk_header Header { get; set; }
        public uint PackageCount { get; set; }
    }
    #endregion

    #region ResTable_map
    [Serializable]
    public class ResTable_map
    {
        public ResTable_ref Name { get; set; }
        public Res_value Value { get; set; }

        public MapMetaAttributes? MetaName
        {
            get
            {
                var ident = (MapMetaAttributes?) Name.Ident;
                switch (ident)
                {
                    case MapMetaAttributes.ATTR_TYPE:
                    case MapMetaAttributes.ATTR_MIN:
                    case MapMetaAttributes.ATTR_MAX:
                    case MapMetaAttributes.ATTR_L10N:
                    case MapMetaAttributes.ATTR_OTHER:
                    case MapMetaAttributes.ATTR_ZERO:
                    case MapMetaAttributes.ATTR_ONE:
                    case MapMetaAttributes.ATTR_TWO:
                    case MapMetaAttributes.ATTR_FEW:
                    case MapMetaAttributes.ATTR_MANY:
                        return ident;
                    default:
                        return null;
                }
            }
            set
            {
                if (value != null)
                {
                    Name.Ident = (uint) value.Value;
                }
                else if (MetaName != null)
                {
                    Name.Ident = 0;
                }
            }
        }

        public MapAllowedTypes? AllowedTypes
        {
            get
            {
                if (MetaName != MapMetaAttributes.ATTR_TYPE) return null;
                return (MapAllowedTypes?) Value.RawData;
            }
            set
            {
                if (MetaName != MapMetaAttributes.ATTR_TYPE)
                {
                    throw new InvalidOperationException(
                        "Can't set AllowedTypes unless MetaName is ATTR_TYPE (0x01000000)");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                Value.RawData = (uint) value.Value;
            }
        }

        public MapL10N? L10N
        {
            get
            {
                if (MetaName != MapMetaAttributes.ATTR_L10N) return null;
                return (MapL10N?) Value.RawData;
            }
            set
            {
                if (MetaName != MapMetaAttributes.ATTR_L10N)
                {
                    throw new InvalidOperationException(
                        "Can't set L10N unless MetaName is ATTR_L10N (0x01000003)");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                Value.RawData = (uint) value.Value;
            }
        }
    }

    public enum MapMetaAttributes
    {
        ATTR_TYPE = 0x01000000,
        ATTR_MIN = 0x01000001,
        ATTR_MAX = 0x01000002,
        ATTR_L10N = 0x01000003,
        ATTR_OTHER = 0x01000004,
        ATTR_ZERO = 0x01000005,
        ATTR_ONE = 0x01000006,
        ATTR_TWO = 0x01000007,
        ATTR_FEW = 0x01000008,
        ATTR_MANY = 0x01000009
    }

    [Flags]
    public enum MapAllowedTypes
    {
        TYPE_ANY = 0x0000FFFF,
        TYPE_REFERENCE = 1 << 0,
        TYPE_STRING = 1 << 1,
        TYPE_INTEGER = 1 << 2,
        TYPE_BOOLEAN = 1 << 3,
        TYPE_COLOR = 1 << 4,
        TYPE_FLOAT = 1 << 5,
        TYPE_DIMENSION = 1 << 6,
        TYPE_FRACTION = 1 << 7,
        TYPE_ENUM = 1 << 16,
        TYPE_FLAGS = 1 << 17
    }

    public enum MapL10N
    {
        L10N_NOT_REQUIRED = 0,
        L10N_SUGGESTED = 1
    }
    #endregion

    #region ResTable_map_entry
    [Serializable]
    public class ResTable_map_entry : ResTable_entry
    {
        public ResTable_ref Parent { get; set; }
        public uint Count { get; set; }
    }
    #endregion

    #region ResTable_package
    [Serializable]
    public class ResTable_package
    {
        public ResChunk_header Header { get; set; }
        public uint Id { get; set; }
        public string Name { get; set; } // 128 x char16_t
        public uint TypeStrings { get; set; }
        public uint LastPublicType { get; set; }
        public uint KeyStrings { get; set; }
        public uint LastPublicKey { get; set; }
    }
    #endregion

    #region ResTable_ref
    [Serializable]
    public class ResTable_ref
    {
        public uint? Ident { get; set; }
    }
    #endregion

    #region ResTable_type
    [Serializable]
    public class ResTable_type
    {
        public ResChunk_header Header { get; set; }
        public uint RawID { get; set; }
        public uint EntryCount { get; set; }
        public uint EntriesStart { get; set; }
        public ResTable_config Config { get; set; }

        public byte ID
        {
            get { return (byte) Helper.GetBits(RawID, 0xFFu, 24); }
            set { RawID = Helper.SetBits(RawID, value, 0xFFu, 24); }
        }

        public byte Res0
        {
            get { return (byte) Helper.GetBits(RawID, 0xFFu, 16); }
            set { RawID = Helper.SetBits(RawID, value, 0xFFu, 16); }
        }

        public ushort Res1
        {
            get { return (ushort) Helper.GetBits(RawID, 0xFFFFu, 0); }
            set { RawID = Helper.SetBits(RawID, value, 0xFFFFu, 0); }
        }
    }
    #endregion

    #region ResTable_typeSpec
    [Serializable]
    public class ResTable_typeSpec
    {
        public ResChunk_header Header { get; set; }
        public uint RawID { get; set; }
        public uint EntryCount { get; set; }

        public byte ID
        {
            get { return (byte) Helper.GetBits(RawID, 0xFFu, 24); }
            set { RawID = Helper.SetBits(RawID, value, 0xFFu, 24); }
        }

        public byte Res0
        {
            get { return (byte) Helper.GetBits(RawID, 0xFFu, 16); }
            set { RawID = Helper.SetBits(RawID, value, 0xFFu, 16); }
        }

        public ushort Res1
        {
            get { return (ushort) Helper.GetBits(RawID, 0xFFFFu, 0); }
            set { RawID = Helper.SetBits(RawID, value, 0xFFFFu, 0); }
        }
    }
    #endregion

    #region ResXMLParser
    public class ResXMLParser
    {
        #region XmlParserEventCode enum

        public enum XmlParserEventCode
        {
            NOT_STARTED,
            BAD_DOCUMENT,
            START_DOCUMENT,
            END_DOCUMENT,
            CLOSED,

            START_NAMESPACE = ResourceType.RES_XML_START_NAMESPACE_TYPE,
            END_NAMESPACE = ResourceType.RES_XML_END_NAMESPACE_TYPE,
            START_TAG = ResourceType.RES_XML_START_ELEMENT_TYPE,
            END_TAG = ResourceType.RES_XML_END_ELEMENT_TYPE,
            TEXT = ResourceType.RES_XML_CDATA_TYPE
        }

        #endregion

        private readonly IEnumerator<XmlParserEventCode> _parserIterator;

        private readonly Stream _source;
        private List<ResXMLTree_attribute> _attributes;
        private object _currentExtension;
        private ResXMLTree_node _currentNode;
        private XmlParserEventCode _eventCode;
        private ResReader _reader;
        private ResResourceMap _resourceMap;
        private ResStringPool _strings;

        public ResXMLParser(Stream source)
        {
            _source = source;
            _reader = new ResReader(_source);
            _eventCode = XmlParserEventCode.NOT_STARTED;
            _parserIterator = ParserIterator().GetEnumerator();
        }

        public ResStringPool Strings
        {
            get { return _strings; }
        }

        public ResResourceMap ResourceMap
        {
            get { return _resourceMap; }
        }

        public XmlParserEventCode EventCode
        {
            get { return _eventCode; }
        }

        public uint? CommentID
        {
            get { return _currentNode == null ? null : _currentNode.Comment.Index; }
        }

        public string Comment
        {
            get { return _strings.GetString(CommentID); }
        }

        public uint? LineNumber
        {
            get { return _currentNode == null ? (uint?) null : _currentNode.LineNumber; }
        }

        public uint? NamespacePrefixID
        {
            get
            {
                var namespaceExt = _currentExtension as ResXMLTree_namespaceExt;
                return namespaceExt == null ? null : namespaceExt.Prefix.Index;
            }
        }

        public string NamespacePrefix
        {
            get { return Strings.GetString(NamespacePrefixID); }
        }

        public uint? NamespaceUriID
        {
            get
            {
                var namespaceExt = _currentExtension as ResXMLTree_namespaceExt;
                return namespaceExt == null ? null : namespaceExt.Uri.Index;
            }
        }

        public string NamespaceUri
        {
            get { return Strings.GetString(NamespaceUriID); }
        }

        public uint? CDataID
        {
            get
            {
                var cdataExt = _currentExtension as ResXMLTree_cdataExt;
                return cdataExt == null ? null : cdataExt.Data.Index;
            }
        }

        public string CData
        {
            get { return Strings.GetString(CDataID); }
        }

        public uint? ElementNamespaceID
        {
            get
            {
                var attrExt = _currentExtension as ResXMLTree_attrExt;
                if (attrExt != null) return attrExt.Namespace.Index;
                var endElementExt = _currentExtension as ResXMLTree_endElementExt;
                if (endElementExt != null) return endElementExt.Namespace.Index;
                return null;
            }
        }

        public string ElementNamespace
        {
            get { return Strings.GetString(ElementNamespaceID); }
        }

        public uint? ElementNameID
        {
            get
            {
                var attrExt = _currentExtension as ResXMLTree_attrExt;
                if (attrExt != null) return attrExt.Name.Index;
                var endElementExt = _currentExtension as ResXMLTree_endElementExt;
                if (endElementExt != null) return endElementExt.Name.Index;
                return null;
            }
        }

        public string ElementName
        {
            get { return Strings.GetString(ElementNameID); }
        }

        public uint? ElementIdIndex
        {
            get
            {
                var attrExt = _currentExtension as ResXMLTree_attrExt;
                if (attrExt != null) return attrExt.IdIndex;
                return null;
            }
        }

        public AttributeInfo ElementId
        {
            get { return GetAttribute(ElementIdIndex); }
        }

        public uint? ElementClassIndex
        {
            get
            {
                var attrExt = _currentExtension as ResXMLTree_attrExt;
                if (attrExt != null) return attrExt.ClassIndex;
                return null;
            }
        }

        public AttributeInfo ElementClass
        {
            get { return GetAttribute(ElementClassIndex); }
        }

        public uint? ElementStyleIndex
        {
            get
            {
                var attrExt = _currentExtension as ResXMLTree_attrExt;
                if (attrExt != null) return attrExt.StyleIndex;
                return null;
            }
        }

        public AttributeInfo ElementStyle
        {
            get { return GetAttribute(ElementStyleIndex); }
        }

        public uint AttributeCount
        {
            get { return _attributes == null ? 0 : (uint) _attributes.Count; }
        }

        public void Restart()
        {
            throw new NotSupportedException();
        }

        public XmlParserEventCode Next()
        {
            if (_parserIterator.MoveNext())
            {
                _eventCode = _parserIterator.Current;
                return _parserIterator.Current;
            }
            _eventCode = XmlParserEventCode.END_DOCUMENT;
            return _eventCode;
        }

        private void ClearState()
        {
            _currentNode = null;
            _currentExtension = null;
            _attributes = null;
        }

        private IEnumerable<XmlParserEventCode> ParserIterator()
        {
            while (true)
            {
                ClearState();
                ResChunk_header header;
                try
                {
                    header = _reader.ReadResChunk_header();
                }
                catch (EndOfStreamException)
                {
                    break;
                }
                var subStream = new BoundedStream(_reader.BaseStream, header.Size - 8);
                var subReader = new ResReader(subStream);
                switch (header.Type)
                {
                    case ResourceType.RES_XML_TYPE:
                        yield return XmlParserEventCode.START_DOCUMENT;
                        _reader = subReader; // Bound whole file
                        continue; // Don't skip content
                    case ResourceType.RES_STRING_POOL_TYPE:
                        ResStringPool_header stringPoolHeader = subReader.ReadResStringPool_header(header);
                        _strings = subReader.ReadResStringPool(stringPoolHeader);
                        break;
                    case ResourceType.RES_XML_RESOURCE_MAP_TYPE:
                        ResResourceMap resourceMap = subReader.ReadResResourceMap(header);
                        _resourceMap = resourceMap;
                        break;
                    case ResourceType.RES_XML_START_NAMESPACE_TYPE:
                        _currentNode = subReader.ReadResXMLTree_node(header);
                        _currentExtension = subReader.ReadResXMLTree_namespaceExt();
                        yield return XmlParserEventCode.START_NAMESPACE;
                        break;
                    case ResourceType.RES_XML_END_NAMESPACE_TYPE:
                        _currentNode = subReader.ReadResXMLTree_node(header);
                        _currentExtension = subReader.ReadResXMLTree_namespaceExt();
                        yield return XmlParserEventCode.END_NAMESPACE;
                        break;
                    case ResourceType.RES_XML_START_ELEMENT_TYPE:
                        _currentNode = subReader.ReadResXMLTree_node(header);
                        ResXMLTree_attrExt attrExt = subReader.ReadResXMLTree_attrExt();
                        _currentExtension = attrExt;

                        _attributes = new List<ResXMLTree_attribute>();
                        for (int i = 0; i < attrExt.AttributeCount; i++)
                        {
                            _attributes.Add(subReader.ReadResXMLTree_attribute());
                        }
                        yield return XmlParserEventCode.START_TAG;
                        break;
                    case ResourceType.RES_XML_END_ELEMENT_TYPE:
                        _currentNode = subReader.ReadResXMLTree_node(header);
                        _currentExtension = subReader.ReadResXMLTree_endElementExt();
                        yield return XmlParserEventCode.END_TAG;
                        break;
                    case ResourceType.RES_XML_CDATA_TYPE:
                        _currentNode = subReader.ReadResXMLTree_node(header);
                        _currentExtension = subReader.ReadResXMLTree_cdataExt();
                        yield return XmlParserEventCode.TEXT;
                        break;
                    default:
                        Console.WriteLine("Warning: Skipping chunk of type {0} (0x{1:x4})",
                                          header.Type, (int) header.Type);
                        break;
                }
                byte[] junk = subStream.ReadFully();
                if (junk.Length > 0)
                {
                    Console.WriteLine("Warning: Skipping {0} bytes at the end of a {1} (0x{2:x4}) chunk.",
                                      junk.Length, header.Type, (int) header.Type);
                }
            }
        }

        public AttributeInfo GetAttribute(uint? index)
        {
            if (index == null || _attributes == null) return null;
            if (index >= _attributes.Count) throw new ArgumentOutOfRangeException("index");
            ResXMLTree_attribute attr = _attributes[(int) index];
            return new AttributeInfo(this, attr);
        }

        public uint? IndexOfAttribute(string ns, string attribute)
        {
            uint? nsID = _strings.IndexOfString(ns);
            uint? nameID = _strings.IndexOfString(attribute);
            if (nameID == null) return null;
            uint index = 0;
            foreach (ResXMLTree_attribute attr in _attributes)
            {
                if (attr.Namespace.Index == nsID && attr.Name.Index == nameID)
                {
                    return index;
                }
                index++;
            }
            return null;
        }

        public void Close()
        {
            if (_eventCode == XmlParserEventCode.CLOSED) return;
            _eventCode = XmlParserEventCode.CLOSED;
            _reader.Close();
        }

        #region Nested type: AttributeInfo

        public class AttributeInfo
        {
            private readonly ResXMLParser _parser;

            public AttributeInfo(ResXMLParser parser, ResXMLTree_attribute attribute)
            {
                _parser = parser;
                TypedValue = attribute.TypedValue;
                ValueStringID = attribute.RawValue.Index;
                NameID = attribute.Name.Index;
                NamespaceID = attribute.Namespace.Index;
            }

            public uint? NamespaceID { get; private set; }

            public string Namespace
            {
                get { return _parser.Strings.GetString(NamespaceID); }
            }

            public uint? NameID { get; private set; }

            public string Name
            {
                get { return _parser.Strings.GetString(NameID); }
            }

            public uint? ValueStringID { get; private set; }

            public string ValueString
            {
                get { return _parser.Strings.GetString(ValueStringID); }
            }

            public Res_value TypedValue { get; private set; }
        }

        #endregion
    }
    #endregion

    #region ResXMLTree_attrExt
    [Serializable]
    public class ResXMLTree_attrExt
    {
        public ResStringPool_ref Namespace { get; set; }
        public ResStringPool_ref Name { get; set; }
        public ushort AttributeStart { get; set; }
        public ushort AttributeSize { get; set; }
        public ushort AttributeCount { get; set; }
        public ushort IdIndex { get; set; }
        public ushort ClassIndex { get; set; }
        public ushort StyleIndex { get; set; }
    }
    #endregion

    #region ResXMLTree_attribute
    [Serializable]
    public class ResXMLTree_attribute
    {
        public ResStringPool_ref Namespace { get; set; }
        public ResStringPool_ref Name { get; set; }
        public ResStringPool_ref RawValue { get; set; }
        public Res_value TypedValue { get; set; }
    }
    #endregion

    #region ResXMLTree_cdataExt
    [Serializable]
    public class ResXMLTree_cdataExt
    {
        public ResStringPool_ref Data { get; set; }
        public Res_value TypedData { get; set; }
    }
    #endregion

    #region ResXMLTree_endElementExt
    [Serializable]
    public class ResXMLTree_endElementExt
    {
        public ResStringPool_ref Namespace { get; set; }
        public ResStringPool_ref Name { get; set; }
    }
    #endregion

    #region ResXMLTree_header
    [Serializable]
    public class ResXMLTree_header
    {
        public ResChunk_header Header { get; set; }
    }
    #endregion

    #region ResXMLTree_namespaceExt
    [Serializable]
    public class ResXMLTree_namespaceExt
    {
        public ResStringPool_ref Prefix { get; set; }
        public ResStringPool_ref Uri { get; set; }
    }
    #endregion

    #region ResXMLTree_node
    [Serializable]
    public class ResXMLTree_node
    {
        public ResChunk_header Header { get; set; }
        public uint LineNumber { get; set; }
        public ResStringPool_ref Comment { get; set; }
    }
    #endregion

    #region ResXMLTree_startelement
    public class ResXMLTree_startelement
    {
        public ResXMLTree_node Node { get; set; }
        public ResXMLTree_attrExt AttrExt { get; set; }
        public List<ResXMLTree_attribute> Attributes { get; set; }
    }
    #endregion
}