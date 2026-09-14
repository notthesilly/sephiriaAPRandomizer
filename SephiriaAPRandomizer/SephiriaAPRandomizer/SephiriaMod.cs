using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Helpers;

[assembly: MelonInfo(
    typeof(SephiriaAPRandomizer.SephiriaMod),
    "Sephiria AP Randomizer",
    "0.1.0",
    "TheSilly"
)]

namespace SephiriaAPRandomizer
{
    public class SephiriaMod : MelonMod
    {
        private ArchipelagoSession session;

        public override void OnInitializeMelon()
        {
            session = ArchipelagoSessionFactory.CreateSession("localhost", 38281);

            session.Items.ItemReceived += OnItemReceived;

            MelonLogger.Msg("Connecting to Archipelago...");

            var result = session.TryConnectAndLogin(
                "Sephiria",
                "Silly",
                ItemsHandlingFlags.AllItems,
                new Version(0, 6, 7)
            );

            if (result.Successful)
            {
                MelonLogger.Msg("Connected to Archipelago!");
            }
            else
            {
                MelonLogger.Msg("Failed to connect to Archipelago");
            }
        }

        private void OnItemReceived(ReceivedItemsHelper helper)
        {
            while (helper.Any())
            {
                var item = helper.DequeueItem();

                MelonLogger.Msg(
                    $"Received item: {item.ItemName} " +
                    $"from player {item.Player} " +
                    $"at location {item.LocationName} "
                );
            }
        }
    }
}