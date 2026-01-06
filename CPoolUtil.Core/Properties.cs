using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CPoolUtil.Core
{
    #region Property types

    [DebuggerDisplay("{Name} = {Data}")]
    /// <summary>
    ///                (             propertyName             )           (              propertyType             )           (         data           (     anything      ))
    /// common format : properyNameLength - propertyName .. \0 - padding - propertyTypeLength - propertyType .. \0 - padding - dataLength - padding |-| eachProperty format
    /// </summary>
    public abstract class CProperty
    {
        public string Name { get; protected set; } = "";
        public string FriendlyName { get; private set; }
        public string Data { get; protected set; }

        public CProperty(string name)
        {
            Name = name;

            // Top level properties
            FriendlyName = Name == "CharacterPool" ? "Character Count"
                : Name == "PoolFileName" ? "File Name"

            // Character properties
                : Name == "strFirstName" ? "First Name"
                : Name == "strLastName" ? "Last Name"
                : Name == "strNickName" ? "Nickname"
                : Name == "m_SoldierClassTemplateName" ? "Soldier Class"
                : Name == "CharacterTemplateName" ? "Character Template"
                : Name == "kAppearance" ? "Appearance"
                : Name == "Country" ? "Country"
                : Name == "AllowedTypeSoldier" ? "Can Be Soldier"
                : Name == "AllowedTypeVIP" ? "Can Be VIP"
                : Name == "AllowedTypeDarkVIP" ? "Can Be Dark VIP"
                : Name == "PoolTimestamp" ? "Created On"
                : Name == "BackgroundText" ? "Biography"

            // Appearance properties
                : Name == "nmHead" ? "Head"
                : Name == "iGender" ? "Gender"
                : Name == "iRace" ? "Race"
                : Name == "nmHaircut" ? "Haircut"
                : Name == "iHairColor" ? "Hair Color"
                : Name == "iFacialHair" ? "Facial Hair"
                : Name == "nmBeard" ? "Beard"
                : Name == "iSkinColor" ? "Skin Color"
                : Name == "iEyeColor" ? "Eye Color"
                : Name == "nmFlag" ? "Flag"
                : Name == "iVoice" ? "Voice"
                : Name == "iAttitude" ? "Attitude"
                : Name == "iArmorDeco" ? "Armor Decor"
                : Name == "iArmorTint" ? "Armor Color"
                : Name == "iArmorTintSecondary" ? "Secondary Armor Color"
                : Name == "iWeaponTint" ? "Weapon Color"
                : Name == "iTattooTint" ? "Tattoo Color"
                : Name == "nmWeaponPattern" ? "Weapon Pattern"
                : Name == "nmPawn" ? "Pawn"
                : Name == "nmTorso" ? "Torso"
                : Name == "nmArms" ? "Arms"
                : Name == "nmLegs" ? "Legs"
                : Name == "nmHelmet" ? "Helmet"
                : Name == "nmEye" ? "Eye"
                : Name == "nmTeeth" ? "Teeth"
                : Name == "nmFacePropLower" ? "Lower Face Prop"
                : Name == "nmFacePropUpper" ? "Upper Face Prop"
                : Name == "nmPatterns" ? "Patterns"
                : Name == "nmVoice" ? "Voice Model"
                : Name == "nmLanguage" ? "Language"
                : Name == "nmTattoo_LeftArm" ? "Left Arm Tattoo"
                : Name == "nmTattoo_RightArm" ? "Right Arm Tattoo"
                : Name == "nmScars" ? "Scars"
                : Name == "nmTorso_Underlay" ? "Torso Underlay"
                : Name == "nmArms_Underlay" ? "Arms Underlay"
                : Name == "nmLegs_Underlay" ? "Legs Underlay"
                : Name == "nmFacePaint" ? "Face Paint"
                : Name == "nmLeftArm" ? "Left Arm"
                : Name == "nmRightArm" ? "Right Arm"
                : Name == "nmLeftArmDeco" ? "Left Arm Decor"
                : Name == "nmRightArmDeco" ? "Right Arm Decor"
                : Name == "nmLeftForearm" ? "Left Forearm"
                : Name == "nmRightForearm" ? "Right Forearm"
                : Name == "nmThighs" ? "Thighs"
                : Name == "nmShins" ? "Shins"
                : Name == "nmTorsoDeco" ? "Torso Decor"
                : Name == "bGhostPawn" ? "Ghost Pawn"

            // Fallback
                : Name + " (Missing Friendly Name)";
        }

        public virtual CProperty Clone()
        {
            return MemberwiseClone() as CProperty;
        }

        public bool IsDuplicate(CProperty otherProperty)
        {
            return otherProperty.Data == Data;
        }

        public virtual string GetPropertyType()
        {
            return GetType().Name;
        }

        public abstract void ParseData(Parser parser);

        public abstract byte[] WriteSizeAndData();
    }

    // Data = bytes
    /// <summary>
    ///                                                                                                              4+byteslength+padding 
    ///         (             propertyName             )           (              propertyType             )           (           data         (          bytes              ))
    /// format : properyNameLength - propertyName .. \0 - padding - propertyTypeLength - propertyType .. \0 - padding - dataLength - padding |-| bytesLength - bytes - padding
    public class ByteProperty : CProperty
    {
        public byte[] Value
        {
            get => _val;
            set
            {
                _val = value;
                Data = value.ToString();
            }
        }
        private byte[] _val;

        public ByteProperty(string name) : base(name) { }

        private ByteProperty(string name, byte[] newValue) : base(name)
        {
            Value = newValue ?? [];
        }

        public static ByteProperty Create(string name, byte[] value = null)
        {
            return new ByteProperty(name, value);
        }

        public override void ParseData(Parser Parser)
        {
            var actualLength = Parser.GetInt();
            Value = Parser.GetBytes(actualLength);
            Parser.SkipPadding();
        }

        public override byte[] WriteSizeAndData()
        {
            var bytes = Value;
            // Size (always int(4)+byteslength+padding(4)), Padding, bytesLength, bytes, Padding
            return [.. Parser.WriteInt(bytes.Length+8), ..Parser.WritePadding(), .. Parser.WriteInt(bytes.Length), .. bytes, .. Parser.WritePadding()];
        }
    }

    public class NoneProperty : CProperty
    {
        public NoneProperty(string name) : base(name)
        {
            Data = "null";
        }
        public override void ParseData(Parser parser)
        {
            // No data to parse
        }
        public override byte[] WriteSizeAndData()
        {
            return [];
        }
    }

    // Data = 1 or 0
    /// <summary>
    ///         (             propertyName             )           (              propertyType             )           ( (always0)     data     (bool 1bit))
    /// format : properyNameLength - propertyName .. \0 - padding - propertyTypeLength - propertyType .. \0 - padding - dataLength - padding |-| boolValue
    public class BoolProperty : CProperty
    {
        public bool Value
        {
            get => _val;
            set
            {
                _val = value;
                Data = value.ToString();
            }
        }
        private bool _val;
        public BoolProperty(string name) : base(name) { }

        private BoolProperty(string name, bool newValue) : this(name)
        {
            Value = newValue;
        }

        public static BoolProperty Create(string name, bool value = false)
        {
            return new BoolProperty(name, value);
        }

        public override void ParseData(Parser Parser)
        {
            // One byte of data
            Value = (Parser.GetBytes(1)[0]) == 1;
        }

        public override byte[] WriteSizeAndData()
        {
            // Size (always 0), Padding, Data
            return [.. Parser.WriteInt(0), .. Parser.WritePadding(), .. BitConverter.GetBytes(Value)];
        }
    }

    // Data = Simple 4 byte number
    /// <summary>
    ///         (             propertyName             )           (              propertyType             )           ( (always4)     data     (   int  ))
    /// format : properyNameLength - propertyName .. \0 - padding - propertyTypeLength - propertyType .. \0 - padding - dataLength - padding |-| intValue
    public class IntProperty : CProperty
    {
        public int Value
        {
            get => _val;
            set
            {
                _val = value;
                Data = value.ToString();
            }
        }
        private int _val;

        public IntProperty(string name) : base(name) { }
        private IntProperty(string name, int newValue) : this(name)
        {
            Value = newValue;
        }

        public static IntProperty Create(string name, int value = 0)
        {
            return new IntProperty(name, value);
        }

        public override void ParseData(Parser Parser)
        {
            Value = Parser.GetInt(); // We know the size is always 4, so just call Getint()
        }

        public override byte[] WriteSizeAndData()
        {
            // Size (4), Padding, Data
            return [.. Parser.WriteInt(4), .. Parser.WritePadding(), .. Parser.WriteInt(Value)];
        }
    }

    // Data = ASCII(?) encoded text block
    /// <summary>
    ///         (             propertyName             )           (              propertyType             )           (         data           (         string        ))
    /// format : properyNameLength - propertyName .. \0 - padding - propertyTypeLength - propertyType .. \0 - padding - dataLength - padding |-| encodeedLength - string 
    public class StrProperty : CProperty
    {
        public string Value
        {
            get => _val;
            set
            {
                value = value ?? "";
                _val = value;
                Data = value.ToString();
            }
        }
        private string _val;

        public StrProperty(string name) : base(name) { }
        private StrProperty(string name, string newValue) : this(name)
        {
            Value = newValue;
        }

        public static StrProperty Create(string name, string value = "")
        {
            return new StrProperty(name, value);
        }

        public override void ParseData(Parser Parser)
        {
            // 使用Parser中的统一解析逻辑
            Value = Parser.ParseStrProperty();
        }

        public override byte[] WriteSizeAndData()
        {
            // 使用新的WriteStrPropertyData方法，根据文本内容自动选择编码格式
            var formatted = Parser.WriteStrPropertyData(Value);

            // Size + 4, Padding, Size, Data
            return [.. Parser.WriteInt(formatted.Length), .. Parser.WritePadding(), .. formatted];
        }
    }

    // Data = Like StrProperty, but with end padding
    /// <summary>
    ///         (             propertyName             )           (              propertyType             )           (         data           (               string                ))
    /// format : properyNameLength - propertyName .. \0 - padding - propertyTypeLength - propertyType .. \0 - padding - dataLength - padding |-| stringLength - string .. \0 - padding
    /// </summary>
    public class NameProperty : CProperty
    {
        public string Value
        {
            get => _val;
            set
            {
                value = value ?? "";
                _val = value;
                Data = value.ToString();
            }
        }
        private string _val;

        public NameProperty(string name) : base(name) { }

        private NameProperty(string name, string newValue) : this(name)
        {
            Value = newValue;
        }

        public static NameProperty Create(string name, string value = "")
        {
            return new NameProperty(name, value);
        }

        public override void ParseData(Parser Parser)
        {
            int actualSize = Parser.GetInt();
            Value = Parser.GetString(actualSize);
            Parser.SkipPadding();
        }

        public override byte[] WriteSizeAndData()
        {
            var formatted = Parser.WriteString(Value);

            // Size + 8, Padding, Size, Data, Padding
            return
            [
                .. Parser.WriteInt(formatted.Length + 8),
                .. Parser.WritePadding(),
                .. Parser.WriteInt(formatted.Length),
                .. formatted,
                .. Parser.WritePadding(),
            ];
        }
    }

    // Data = Number of elements in an array. with elements
    /// <summary>
    ///         (             propertyName             )           (              propertyType             )           (after padding to None<p>  data        (       array elements       ))
    /// format : properyNameLength - propertyName .. \0 - padding - propertyTypeLength - propertyType .. \0 - padding - dataLenght - padding |-| arrayLength - elements(end with None<p>) ..
    /// </summary>
    public class ArrayProperty : CProperty
    {
        public List<PropertyBag> Properties = new();
        public int ArrayLength { get => Properties.Count; }

        public ArrayProperty(string name) : base(name)
        {
        }

        private ArrayProperty(string name, params CProperty[] properties) : base(name)
        {
        }

        public static ArrayProperty Create(string name, params CProperty[] properties)
        {
            return new ArrayProperty(name, properties);
        }

        public override void ParseData(Parser Parser)
        {
            var arrayLength = Parser.GetInt();
            for (int i = 0;i<arrayLength;i++)
            {
                var elem = new PropertyBag();
                elem.ParseData(Parser);
                Properties.Add(elem);
            }

            // For comparison purposes, serialize all Properties name and value into Data
            Data = string.Join(" ", Properties.SelectMany(b => b.Properties.Select(p => $"{p.Name}:{p.Data}")));
        }

        public override byte[] WriteSizeAndData()
        {
            // Get the data of all sub Properties
            byte[] elemBytes = [.. Properties.SelectMany<PropertyBag, byte>(p => [.. p.WriteSizeAndData(), ..Parser.WriteNone()])];
            byte[] totalBytes = [..Parser.WriteInt(ArrayLength), .. elemBytes];

            return [.. Parser.WriteInt(totalBytes.Length), .. Parser.WritePadding(), .. totalBytes];
        }
    }

    // Data = A subcollection of properties
    ///         (             propertyName             )           (              propertyType             )           (after type's padding to None<p>                     data                        (    struct fields   ))
    /// format : properyNameLength - propertyName .. \0 - padding - propertyTypeLength - propertyType .. \0 - padding - dataLength - padding |-| structTypeNameLenght - sturctTypeName .. \0 - padding - eachFields - None<p>
    /// </summary>
    public class StructProperty : CProperty
    {
        public PropertyBag Properties = new();
        public string StructType { get; private set; }

        public StructProperty(string name) : base(name)
        {
        }

        public StructProperty(string name, string typeName) : this(name)
        {
            StructType = typeName;
        }

        protected StructProperty(string name, string typeName, params CProperty[] properties) : this(name, typeName)
        {
            Properties.Properties = [.. properties];
        }

        public static StructProperty Create(string name, string typeName, params CProperty[] properties)
        {
            return new StructProperty(name, typeName, properties);
        }

        public override void ParseData(Parser Parser)
        {
            int structTypeLength = Parser.GetInt();
            StructType = Parser.GetString(structTypeLength);
            Parser.SkipPadding();
            Properties.ParseData(Parser);
        }

        public override byte[] WriteSizeAndData()
        {
            // Get the data of all sub Properties
            byte[] bytes = [.. Properties.Properties.SelectMany(p => Parser.WriteProperty(p)), .. Parser.WriteNone()];
            var bytesType = Parser.WriteString(StructType);

            // Size is the combined length of all the Properties AFTER the TAppearance string, plus our own ending "None"
            byte[] structBytes = [.. Parser.WriteInt(bytes.Length), .. Parser.WritePadding(), .. Parser.WriteInt(bytesType.Length), .. bytesType, .. Parser.WritePadding()];

            // Size (combined length of all the Properties AFTER the TAppearance string, plus our own ending "None"), Padding, TApperance Length & String, All Property Values, "None"
            return [.. structBytes, .. bytes];
        }
    }

    #endregion

    public class PropertyBag
    {
        public List<CProperty> Properties = [];
        public string DisplayValue { get; protected set; }

        public PropertyBag() { }
        protected PropertyBag(params CProperty[] pProperties)
        {
            Properties = [.. pProperties];
        }

        public static PropertyBag Create(params CProperty[] pProperties)
        {
            return new PropertyBag(pProperties);
        }

        public virtual PropertyBag Clone()
        {
            var cloned = MemberwiseClone() as PropertyBag;
            cloned.Properties = [.. Properties.Select(p => p.Clone())];
            return cloned;
        }

        public void ParseData(Parser Parser)
        {
            Properties = [];
            // Keep reading until we hit a "None" property
            CProperty curProp;
            do
            {
                curProp = Parser.GetProperty();
                if (curProp != null) Properties.Add(curProp);
            } while (curProp != null);

            DisplayValue = string.Join(" ", Properties.Select(p => $"{p.Name}:{p.Data}"));
        }

        public virtual byte[] WriteSizeAndData()
        {
            byte[] bytes = [.. Properties.SelectMany(p => Parser.WriteProperty(p))];

            return [.. bytes];
        }

        public void WriteDebug(IOutputter Outputter, int curTabLevel)
        {
            string tabs = string.Join("", Enumerable.Repeat("\t", curTabLevel));

            foreach (var prop in Properties)
            {
                Outputter.WriteLine($"{tabs}{prop.FriendlyName}: {prop.Data}");

                if (prop is StructProperty sProp)
                    sProp.Properties.WriteDebug(Outputter, ++curTabLevel);
                else if (prop is ArrayProperty aProp)
                {
                    foreach(var propertyBag in aProp.Properties)
                    {
                        propertyBag.WriteDebug(Outputter, ++curTabLevel);
                    }
                }
                else if (prop is CharacterArray cProp)
                {
                    cProp.Header.WriteDebug(Outputter, ++curTabLevel);
                    foreach (var propertyBag in cProp.Properties)
                    {
                        propertyBag.WriteDebug(Outputter, ++curTabLevel);
                    }
                }
            }
        }
    }
}