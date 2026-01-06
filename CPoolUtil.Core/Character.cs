using System;
using System.Collections.Generic;
using System.Linq;

namespace CPoolUtil.Core
{
    // Data = Number of elements in an array. Same format as IntProperty
    /// <summary>
    ///         (             propertyName             )           (              propertyType             )           (    4    data           (                           array elements                             ))
    /// format : properyNameLength - propertyName .. \0 - padding - propertyTypeLength - propertyType .. \0 - padding - dataLenght - padding |-| arrayLength - header(struct) - arrayLength - elements(end with None<p>) ..
    /// </summary>
    public class CharacterArray : CProperty
    {
        public PropertyBag Header = new();
        public List<PropertyBag> Properties = new();
        public int ArrayLength { get => Properties.Count; }

        private CharacterArray(string name, PropertyBag header, List<PropertyBag> elements) : base(name)
        {
            Header = header;
            Properties = elements;
            UpdateDisplayValues();
        }

        public CharacterArray(string name) : base(name)
        {
            Header = new PropertyBag();
            Properties = [];
            UpdateDisplayValues();
        }

        public static CharacterArray Create(string name, PropertyBag header, List<PropertyBag> elements)
        {
            return new CharacterArray(name, header, elements);
        }

        public override string GetPropertyType()
        {
            return "ArrayProperty";
        }

        public override void ParseData(Parser Parser)
        {
            var arrayLength = Parser.GetInt();
            Header.ParseData(Parser);
            Parser.GetInt();
            for (int i = 0; i < arrayLength; i++)
            {
                var elem = new Character();
                elem.ParseData(Parser);
                //elem.WriteDebug(outputter, 1);
                //elem.Appearance = new Appearance(elem.KAppearance.Properties);
                Properties.Add(elem);
            }
            UpdateDisplayValues();
        }

        public override byte[] WriteSizeAndData()
        {
            // Get the data of all sub Properties
            byte[] headerBytes = [.. Header.WriteSizeAndData(), .. Parser.WriteNone()];
            byte[] elemBytes = [.. Properties.SelectMany<PropertyBag, byte>(p => [.. p.WriteSizeAndData(), .. Parser.WriteNone()])];

            return [.. Parser.WriteInt(4), .. Parser.WritePadding(), .. Parser.WriteInt(ArrayLength), .. headerBytes, .. Parser.WriteInt(ArrayLength), .. elemBytes];
        }

        public void UpdateCharacterList(List<Character> characters)
        {
            Properties = [.. characters.Select(c => c as PropertyBag)];
            UpdateDisplayValues();
        }

        public void UpdateDisplayValues()
        {
            // For comparison purposes, serialize all Properties name and value into Data
            Data = string.Join(" ", Header.Properties.Select(p => $"{p.Name}:{p.Data}"), Properties.SelectMany(p => (p as PropertyBag).Properties.Select(q => $"{q.Name}:{q.Data}")));
        }
    }

    public class Character : PropertyBag
    {
        public Guid ID { get; private set; } // For easier lookup
        public StrProperty FirstName => Properties.FirstOrDefault(p => p.Name == "strFirstName") as StrProperty;
        public StrProperty LastName => Properties.FirstOrDefault(p => p.Name == "strLastName") as StrProperty;
        public StrProperty NickName => Properties.FirstOrDefault(p => p.Name == "strNickName") as StrProperty;
        public string FullName => $"{FirstName?.Value} {(string.IsNullOrWhiteSpace(NickName?.Value) ? string.Empty : "\'" + NickName.Value + "\'")} {LastName?.Value}".Trim().Replace("  ", " ") + (IsDirty ? "*" : "");
        //public string FullName => $"{FirstName?.Value} {NickName?.Value} {LastName?.Value}".Trim().Replace("  ", " ") + (IsDirty ? "*" : "");
        public NameProperty SoldierType => Properties.FirstOrDefault(p => p.Name == "CharacterTemplateName") as NameProperty;
        public NameProperty PreferredClass => Properties.FirstOrDefault(p => p.Name == "m_SoldierClassTemplateName") as NameProperty;
        public NameProperty Country => Properties.FirstOrDefault(p => p.Name == "Country") as NameProperty;
        public Appearance Appearance => Properties.FirstOrDefault(p => p.Name == "kAppearance") as Appearance;
        public BoolProperty CanBeSoldier => Properties.FirstOrDefault(p => p.Name == "AllowedTypeSoldier") as BoolProperty;
        public BoolProperty CanBeVIP => Properties.FirstOrDefault(p => p.Name == "AllowedTypeVIP") as BoolProperty;
        public BoolProperty CanBeDarkVIP => Properties.FirstOrDefault(p => p.Name == "AllowedTypeDarkVIP") as BoolProperty;
        public StrProperty CreatedOn => Properties.FirstOrDefault(p => p.Name == "PoolTimestamp") as StrProperty;
        public StrProperty Biography => Properties.FirstOrDefault(p => p.Name == "BackgroundText") as StrProperty;

        public bool IsDirty { get; set; }

        public Character() : base()
        {
            ID = Guid.NewGuid();
        }

        private Character(string nickname) : this()
        {
            Properties.Add(StrProperty.Create("strFirstName", "New"));
            Properties.Add(StrProperty.Create("strLastName", ""));
            Properties.Add(StrProperty.Create("strNickName", nickname));
            Properties.Add(NameProperty.Create("CharacterTemplateName", "Soldier"));
            Properties.Add(NameProperty.Create("m_SoldierClassTemplateName", "Rookie"));
            Properties.Add(NameProperty.Create("Country", "Country_China"));
            Properties.Add(Appearance.CreateAppearanceProperty());
            Properties.Add(BoolProperty.Create("AllowedTypeSoldier", true));
            Properties.Add(BoolProperty.Create("AllowedTypeVIP"));
            Properties.Add(BoolProperty.Create("AllowedTypeDarkVIP"));
            Properties.Add(StrProperty.Create("PoolTimestamp", DateTime.Now.ToString("MMMM d, yyyy - h:m tt")));
            Properties.Add(StrProperty.Create("BackgroundText"));
            IsDirty = true;
        }

        public new Character Clone()
        {
            return base.Clone() as Character;
        }

        public bool IsDuplicate(Character otherCharacter)
        {
            // Return false as soon as any discrepency is detected
            if (otherCharacter.Properties.Count != Properties.Count)
                return false;

            foreach (var ourProp in Properties)
                if (otherCharacter.Properties.FirstOrDefault(p => p.Name == ourProp.Name)?.Data != ourProp.Data)
                    return false;

            return true;
        }

        public static Character Create(string nickName = "")
        {
            return new Character(nickName);
        }
    }

    public class Appearance : StructProperty
    {
        #region Properties
        public NameProperty Head => Properties.Properties.FirstOrDefault(p => p.Name == "nmHead") as NameProperty;
        public IntProperty Gender => Properties.Properties.FirstOrDefault(p => p.Name == "iGender") as IntProperty;
        public IntProperty Race => Properties.Properties.FirstOrDefault(p => p.Name == "iRace") as IntProperty;
        public NameProperty Haircut => Properties.Properties.FirstOrDefault(p => p.Name == "nmHaircut") as NameProperty;
        public IntProperty HairColor => Properties.Properties.FirstOrDefault(p => p.Name == "iHairColor") as IntProperty;
        public IntProperty FacialHair => Properties.Properties.FirstOrDefault(p => p.Name == "iFacialHair") as IntProperty;
        public NameProperty Beard => Properties.Properties.FirstOrDefault(p => p.Name == "nmBeard") as NameProperty;
        public IntProperty SkinColor => Properties.Properties.FirstOrDefault(p => p.Name == "iSkinColor") as IntProperty;
        public IntProperty EyeColor => Properties.Properties.FirstOrDefault(p => p.Name == "iEyeColor") as IntProperty;
        public NameProperty Flag => Properties.Properties.FirstOrDefault(p => p.Name == "nmFlag") as NameProperty;
        public IntProperty IVoice => Properties.Properties.FirstOrDefault(p => p.Name == "iVoice") as IntProperty;
        public IntProperty Attitude => Properties.Properties.FirstOrDefault(p => p.Name == "iAttitude") as IntProperty;
        public IntProperty ArmorDeco => Properties.Properties.FirstOrDefault(p => p.Name == "iArmorDeco") as IntProperty;
        public IntProperty ArmorTint1 => Properties.Properties.FirstOrDefault(p => p.Name == "iArmorTint") as IntProperty;
        public IntProperty ArmorTint2 => Properties.Properties.FirstOrDefault(p => p.Name == "iArmorTintSecondary") as IntProperty;
        public IntProperty WeaponTint => Properties.Properties.FirstOrDefault(p => p.Name == "iWeaponTint") as IntProperty;
        public IntProperty TattooTint => Properties.Properties.FirstOrDefault(p => p.Name == "iTattooTint") as IntProperty;
        public NameProperty WeaponPattern => Properties.Properties.FirstOrDefault(p => p.Name == "nmWeaponPattern") as NameProperty;
        public NameProperty Pawn => Properties.Properties.FirstOrDefault(p => p.Name == "nmPawn") as NameProperty;
        public NameProperty Torso => Properties.Properties.FirstOrDefault(p => p.Name == "nmTorso") as NameProperty;
        public NameProperty Arms => Properties.Properties.FirstOrDefault(p => p.Name == "nmArms") as NameProperty;
        public NameProperty Legs => Properties.Properties.FirstOrDefault(p => p.Name == "nmLegs") as NameProperty;
        public NameProperty Helmet => Properties.Properties.FirstOrDefault(p => p.Name == "nmHelmet") as NameProperty;
        public NameProperty Eye => Properties.Properties.FirstOrDefault(p => p.Name == "nmEye") as NameProperty;
        public NameProperty Teeth => Properties.Properties.FirstOrDefault(p => p.Name == "nmTeeth") as NameProperty;
        public NameProperty FacePropLower => Properties.Properties.FirstOrDefault(p => p.Name == "nmFacePropLower") as NameProperty;
        public NameProperty FacePropUpper => Properties.Properties.FirstOrDefault(p => p.Name == "nmFacePropUpper") as NameProperty;
        public NameProperty ArmorPatterns => Properties.Properties.FirstOrDefault(p => p.Name == "nmPatterns") as NameProperty;
        public NameProperty Voice => Properties.Properties.FirstOrDefault(p => p.Name == "nmVoice") as NameProperty;
        public NameProperty Language => Properties.Properties.FirstOrDefault(p => p.Name == "nmLanguage") as NameProperty;
        public NameProperty Tattoo_LeftArm => Properties.Properties.FirstOrDefault(p => p.Name == "nmTattoo_LeftArm") as NameProperty;
        public NameProperty Tattoo_RightArm => Properties.Properties.FirstOrDefault(p => p.Name == "nmTattoo_RightArm") as NameProperty;
        public NameProperty Scars => Properties.Properties.FirstOrDefault(p => p.Name == "nmScars") as NameProperty;
        public NameProperty TorsoUnderlay => Properties.Properties.FirstOrDefault(p => p.Name == "nmTorso_Underlay") as NameProperty;
        public NameProperty ArmsUnderlay => Properties.Properties.FirstOrDefault(p => p.Name == "nmArms_Underlay") as NameProperty;
        public NameProperty LegsUnderlay => Properties.Properties.FirstOrDefault(p => p.Name == "nmLegs_Underlay") as NameProperty;
        public NameProperty FacePaint => Properties.Properties.FirstOrDefault(p => p.Name == "nmFacePaint") as NameProperty;
        public NameProperty LeftArm => Properties.Properties.FirstOrDefault(p => p.Name == "nmLeftArm") as NameProperty;
        public NameProperty RightArm => Properties.Properties.FirstOrDefault(p => p.Name == "nmRightArm") as NameProperty;
        public NameProperty LeftArmDeco => Properties.Properties.FirstOrDefault(p => p.Name == "nmLeftArmDeco") as NameProperty;
        public NameProperty RightArmDeco => Properties.Properties.FirstOrDefault(p => p.Name == "nmRightArmDeco") as NameProperty;
        public NameProperty LeftForearm => Properties.Properties.FirstOrDefault(p => p.Name == "nmLeftForearm") as NameProperty;
        public NameProperty RightForearm => Properties.Properties.FirstOrDefault(p => p.Name == "nmRightForearm") as NameProperty;
        public NameProperty Thighs => Properties.Properties.FirstOrDefault(p => p.Name == "nmThighs") as NameProperty;
        public NameProperty Shins => Properties.Properties.FirstOrDefault(p => p.Name == "nmShins") as NameProperty;
        public NameProperty TorsoDeco => Properties.Properties.FirstOrDefault(p => p.Name == "nmTorsoDeco") as NameProperty;
        public BoolProperty GhostPawn => Properties.Properties.FirstOrDefault(p => p.Name == "bGhostPawn") as BoolProperty;
        #endregion

        public Appearance() : base("kAppearance", "TAppearance")
        {

        }

        public Appearance(CProperty[] appearanceProps) : base("kAppearance", "TAppearance", appearanceProps)
        {
            // Create any Properties that aren't guaranteed to exist (some DLCs added more Properties)
            if (TorsoDeco == null) Properties.Properties.Add(NameProperty.Create("nmTorsoDeco"));
            if (LeftForearm == null) Properties.Properties.Add(NameProperty.Create("nmLeftForearm"));
            if (RightForearm == null) Properties.Properties.Add(NameProperty.Create("nmRightForearm"));
            if (LeftArmDeco == null) Properties.Properties.Add(NameProperty.Create("nmLeftArmDeco"));
            if (RightArmDeco == null) Properties.Properties.Add(NameProperty.Create("nmRightArmDeco"));
            if (Thighs == null) Properties.Properties.Add(NameProperty.Create("nmThighs"));
            if (Shins == null) Properties.Properties.Add(NameProperty.Create("nmShins"));
        }

        public static Appearance CreateAppearanceProperty()
        {
            var props = new CProperty[]
            {
                NameProperty.Create("nmHead"),
                IntProperty.Create("iGender", 2),
                IntProperty.Create("iRace", 2),
                NameProperty.Create("nmHaircut"),
                IntProperty.Create("iHairColor"),
                IntProperty.Create("iFacialHair"),
                NameProperty.Create("nmBeard"),
                IntProperty.Create("iSkinColor"),
                IntProperty.Create("iEyeColor"),
                NameProperty.Create("nmFlag"),
                IntProperty.Create("iVoice"),
                IntProperty.Create("iAttitude"),
                IntProperty.Create("iArmorDeco"),
                IntProperty.Create("iArmorTint"),
                IntProperty.Create("iArmorTintSecondary"),
                IntProperty.Create("iWeaponTint"),
                IntProperty.Create("iTattooTint"),
                NameProperty.Create("nmWeaponPattern"),
                NameProperty.Create("nmPawn", "None"),
                NameProperty.Create("nmTorso"),
                NameProperty.Create("nmArms"),
                NameProperty.Create("nmLegs"),
                NameProperty.Create("nmHelmet"),
                NameProperty.Create("nmEye", "DefaultEyes"),
                NameProperty.Create("nmTeeth", "DefaultTeeth"),
                NameProperty.Create("nmFacePropLower"),
                NameProperty.Create("nmFacePropUpper"),
                NameProperty.Create("nmPatterns"),
                NameProperty.Create("nmVoice"),
                NameProperty.Create("nmLanguage", "None"),
                NameProperty.Create("nmTattoo_LeftArm"),
                NameProperty.Create("nmTattoo_RightArm"),
                NameProperty.Create("nmScars"),
                NameProperty.Create("nmTorso_Underlay"),
                NameProperty.Create("nmArms_Underlay"),
                NameProperty.Create("nmLegs_Underlay"),
                NameProperty.Create("nmFacePaint"),
                NameProperty.Create("nmLeftArm"),
                NameProperty.Create("nmRightArm"),
                NameProperty.Create("nmLeftArmDeco"),
                NameProperty.Create("nmRightArmDeco"),
                NameProperty.Create("nmLeftForearm"),
                NameProperty.Create("nmRightForearm"),
                NameProperty.Create("nmThighs"),
                NameProperty.Create("nmShins", "None"),
                NameProperty.Create("nmTorsoDeco")
            };
            var structProp = new Appearance(props);

            return structProp;
        }

        public override string GetPropertyType()
        {
            return "StructProperty";
        }
    }
}