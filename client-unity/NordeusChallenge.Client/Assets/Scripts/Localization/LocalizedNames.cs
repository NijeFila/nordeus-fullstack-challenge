using NordeusChallenge.Client.Models;

namespace NordeusChallenge.Client.Localization
{
    // Small helpers that resolve localized display strings for server-provided
    // game data. The server English text is always passed as the fallback so
    // missing keys never produce empty labels.
    public static class LocalizedNames
    {
        // ---------- Moves ----------
        public static string Name(MoveDto move)
        {
            if (move == null) return string.Empty;
            return Loc.Tr($"move.{move.id}.name", move.name);
        }

        public static string Description(MoveDto move)
        {
            if (move == null) return string.Empty;
            return Loc.Tr($"move.{move.id}.description", move.description);
        }

        // ---------- Monsters ----------
        public static string Name(MonsterDto monster)
        {
            if (monster == null) return string.Empty;
            return Loc.Tr($"monster.{monster.id}.name", monster.name);
        }

        // ---------- Items ----------
        public static string Name(ItemDto item)
        {
            if (item == null) return string.Empty;
            return Loc.Tr($"item.{item.id}.name", item.name);
        }

        public static string Description(ItemDto item)
        {
            if (item == null) return string.Empty;
            return Loc.Tr($"item.{item.id}.description", item.description);
        }

        // ---------- Environments ----------
        public static string Name(BattleEnvironmentDto environment)
        {
            if (environment == null) return string.Empty;
            return Loc.Tr($"environment.{environment.id}.name", environment.name);
        }

        public static string Description(BattleEnvironmentDto environment)
        {
            if (environment == null) return string.Empty;
            return Loc.Tr($"environment.{environment.id}.description", environment.description);
        }

        // ---------- Shop offers ----------
        public static string Name(ShopOfferDto offer)
        {
            if (offer == null) return string.Empty;
            return Loc.Tr($"shop.{offer.id}.name", offer.name);
        }

        public static string Description(ShopOfferDto offer)
        {
            if (offer == null) return string.Empty;
            return Loc.Tr($"shop.{offer.id}.description", offer.description);
        }

        // ---------- Hero ----------
        public static string Name(HeroDto hero)
        {
            if (hero == null) return string.Empty;
            return Loc.Tr($"hero.{hero.id}.name", hero.name);
        }

        // ---------- Level-up choices ----------
        public static string Name(LevelUpChoiceDto choice)
        {
            if (choice == null) return string.Empty;
            return Loc.Tr($"levelup.{choice.id}.name", choice.name);
        }

        public static string Description(LevelUpChoiceDto choice)
        {
            if (choice == null) return string.Empty;
            return Loc.Tr($"levelup.{choice.id}.description", choice.description);
        }

        // ---------- Generic vocabulary ----------
        public static string Slot(string slot) =>
            Loc.Tr($"slot.{slot}", Capitalize(slot));

        public static string Rarity(string rarity) =>
            Loc.Tr($"rarity.{rarity}", Capitalize(rarity));

        public static string Category(string category) =>
            Loc.Tr($"category.{category}", category);

        public static string Stat(string stat) =>
            Loc.Tr($"stat.{stat}", stat);

        public static string OfferType(string type) =>
            Loc.Tr($"shop.type.{type}", type);

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
