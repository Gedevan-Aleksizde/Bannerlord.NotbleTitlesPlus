using System;
using NobleTitlesPlus.MCMSettings;

namespace NobleTitlesPlus
{
    internal enum NameDisplayLocation
    {
        Original,
        EncyclopediaList,
        EncyclopediaHero,
        Conversation,
        Settlement,
        Map,
        Links,
        Other
    }

    // A scoped override lets a screen opt out even when names elsewhere are enabled.
    // Finalizers on the UI patches restore the previous scope, including on exceptions.
    internal static class NameDisplay
    {
        [ThreadStatic] private static NameDisplayLocation? current;
        internal static NameDisplayLocation Current => current ?? NameDisplayLocation.Other;

        internal static bool Enabled
        {
            get
            {
                var options = MCMRuntimeSettings.Instance?.Options;
                if (options is null) return false;
                return Current switch
                {
                    NameDisplayLocation.EncyclopediaList => options.ShowOnEncyclopediaList,
                    NameDisplayLocation.EncyclopediaHero => options.ShowOnEncyclopediaHero,
                    NameDisplayLocation.Conversation => options.ShowOnConversation,
                    NameDisplayLocation.Settlement => options.ShowOnSettlementUI,
                    NameDisplayLocation.Map => options.ShowOnPartyTooltip,
                    NameDisplayLocation.Links => options.ShowOnHeroLinks,
                    NameDisplayLocation.Other => options.ShowOnOtherScreens,
                    _ => false
                };
            }
        }

        internal static IDisposable Enter(NameDisplayLocation location) => new Scope(location);

        private sealed class Scope : IDisposable
        {
            private readonly NameDisplayLocation? previous;
            private bool disposed;
            internal Scope(NameDisplayLocation location)
            {
                previous = current;
                current = location;
            }
            public void Dispose()
            {
                if (disposed) return;
                current = previous;
                disposed = true;
            }
        }
    }
}
