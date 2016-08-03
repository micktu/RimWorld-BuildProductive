using UnityEngine;
using Verse;

namespace BuildProductive
{
    internal static class VerseExtensions
    {
        internal static Color Command_get_IconDrawColor(this Command command)
        {
            var des = Bootstrapper.CopyDesignator;

            if (command.icon == des.icon && command.hotKey == des.hotKey)
            {
                return des.PublicIconDrawColor;
            }

            // FIXME If Command.IconDrawColor becomes public, return command.IconDrawColor
            return Color.white;
        }
    }
}
