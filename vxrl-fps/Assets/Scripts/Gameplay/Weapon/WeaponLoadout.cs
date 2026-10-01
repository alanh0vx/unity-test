namespace Unity.MP_FPS
{
    /// <summary>
    /// Maps a character + weapon slot to a weapon id in the <see cref="WeaponRegistry"/>.
    /// Slot 0 = ranged (press "1"), slot 1 = melee (press "2").
    ///
    /// Registry ids: 2 Katana, 3 Tanto, 4 Shuriken, 5 Yari spear-launcher.
    /// </summary>
    public static class WeaponLoadout
    {
        public const int RangedSlot = 0;
        public const int MeleeSlot = 1;

        public static uint GetWeaponId(int characterIndex, int slot)
        {
            if (characterIndex == 0) // Ninja
            {
                return slot == RangedSlot ? 4u /* Shuriken */ : 3u /* Tanto */;
            }

            // Shogun (characterIndex 1)
            return slot == RangedSlot ? 5u /* Yari spear-launcher */ : 2u /* Katana */;
        }
    }
}
