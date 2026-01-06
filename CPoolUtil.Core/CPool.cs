using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CPoolUtil.Core
{
    public class CPool : PropertyBag
    {
        protected IOutputter Outputter;
        private List<Character> _characters = [];
        private List<Character> _unchangedCharacterList = []; // Allows resets without reloading the entire pool

        public CharacterArray CharacterPoolProp => Properties.FirstOrDefault(p => p.Name == "CharacterPool") as CharacterArray; // Won't exist if file has no characters
        public int NumCharacters => CharacterPoolProp?.ArrayLength ?? 0;
        public StrProperty PoolFileName => CharacterPoolProp?.Header.Properties.FirstOrDefault(p => p.Name == "PoolFileName") as StrProperty;
        public IReadOnlyList<Character> Characters => _characters;
        public bool IsDirty { get; set; }

        public event Action<int> SendProgressUpdateEvent;
        public event Action<List<Character>> DuplicateCharactersIgnoredEvent;

        public CPool(IOutputter outputter)
        {
            Outputter = outputter;
        }

        public static CPool Create(IOutputter outputter)
        {
            var pool = new CPool(outputter);
            var header = PropertyBag.Create(StrProperty.Create("PoolFileName", "Unsaved Pool"));
            pool.Properties.Add(CharacterArray.Create("CharacterPool", header, []));
            pool.IsDirty = true;
            return pool;
        }

        public void Load(Parser _parser)
        {
            ReadFile(_parser);
            Outputter.WriteLine();
            WriteDebug(Outputter, 0);

            if (_characters.Count > 0) return;

            for (int i = 0; i < NumCharacters; i++)
            {
                Outputter.WriteLine();
                Outputter.WriteLine($"Character {i + 1}");
                var newChar = CharacterPoolProp.Properties[i] as Character;
                _characters.Add(newChar);
                _unchangedCharacterList.Add(newChar.Clone());

                // Send progress update event
                int progress = (int)Math.Round(((i + 1f) / NumCharacters) * 100);
                SendProgressUpdateEvent?.Invoke(progress);
            }
        }

        public void Save(string filePath)
        {
            Outputter.WriteLine($"Saving character pool to {filePath}...");

            byte[] headerBytes = [0xFF, 0xFF, 0xFF, 0xFF];
            CharacterPoolProp.UpdateCharacterList([.. Characters.AsEnumerable()]);
            byte[] fileBytes = [.. headerBytes, .. WriteSizeAndData()];

            File.WriteAllBytes(filePath, [.. fileBytes]);
        }

        private void ReadFile(Parser _parser)
        {
            // Header always consists of 4 0xFF bytes
            var header = _parser.GetBytes(4);
            if (!Enumerable.SequenceEqual(header, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }))
                throw new Exception("Invalid file header.");

            // Get CharacterPool [unless empty] and PoolFileName
            // NOTE: Occasionally the header has a "CharacterPoolSerializeHelper" StructProperty containing 1(?) character
            // This may cause problems if there is ever more than 1, but for now it looks like we can simply throw it away (the character also shows up in the list)
            ParseData(_parser);
        }

        public void AppendCharacters(params Character[] characters)
        {
            if (characters == null)
                return;

            var duplicates = new List<Character>();
            foreach (var newCharacter in characters)
            {
                // Don't append duplicates
                if (Characters.Any(c => c.IsDuplicate(newCharacter)))
                    duplicates.Add(newCharacter);
                else
                {
                    _characters.Add(newCharacter);
                    _unchangedCharacterList.Add(newCharacter.Clone());
                    IsDirty = true;
                }
            }

            if (duplicates.Count != 0)
                DuplicateCharactersIgnoredEvent?.Invoke(duplicates);

            UpdateNumCharactersProperty();
        }

        public void RemoveCharacters(params Character[] characters)
        {
            if (characters == null)
                return;

            foreach (var character in characters)
            {
                _characters.Remove(character);
                _unchangedCharacterList.Remove(_unchangedCharacterList.First(uc => uc.ID == character.ID));
            }

            IsDirty = true;
            UpdateNumCharactersProperty();
        }

        public void ResetCharacters(params Character[] characters)
        {
            if (characters == null)
                return;

            foreach (var character in characters)
            {
                // Clone from the clone to keep them separated and reload original property values. Don't reset "new" characters
                var originalCharacter = _unchangedCharacterList.FirstOrDefault(uc => uc.ID == character.ID).Clone();
                if (!originalCharacter.IsDirty)
                {
                    _characters.Remove(character);
                    _characters.Add(originalCharacter);
                }
            }
        }

        private void UpdateNumCharactersProperty()
        {
            // Update CharacterPool property to reflect new number of characters
            // CharacterPoolProp.Data = $"{{\"NumCharacters\":{NumCharacters}}}";
        }
    }
}