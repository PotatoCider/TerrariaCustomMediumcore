﻿using System;
using System.Collections.Generic;
using System.IO;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Terraria.DataStructures;

namespace CustomMediumcore
{
    [ApiVersion(2, 1)]
    public class CustomMediumcore : TerrariaPlugin
    {
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Miffyli";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "A plugin for customizing what items are dropped on death (drops resources, nothing else)";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "CustomMediumcore";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(1, 4, 0, 1);

        /// <summary>
        /// Name of the file which contains Item IDs that should be dropped on death, one per line
        /// </summary>
        public const string ItemIDFile = "drop_item_ids.txt";

        public Item[] DropItems;

        /// <summary>
        /// Initializes a new instance of the CustomMediumcore class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public CustomMediumcore(Main game) : base(game)
        {
            // Read item IDs that should be dropped, and turn them into items
            string[] idStrings = System.IO.File.ReadAllLines(ItemIDFile);
            var ItemList = new List<Item>();
            foreach (string idString in idStrings)
            {
                int itemId = int.Parse(idString);
                ItemList.Add(TShock.Utils.GetItemById(itemId));
            }
            DropItems = ItemList.ToArray();
            Console.WriteLine("[CustomMediumcore] Loaded {0} items for dropping on death", idStrings.Length);
        }

        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, onGetData);
        }


        /// <summary>
        /// Handles plugin disposal logic.
        /// *Supposed* to fire when the server shuts down.
        /// You should deregister hooks and free all resources here.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, onGetData);
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Checks incoming packets and manually hooks
        /// to packets of interest
        /// </summary>
        public void onGetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.PlayerDeathV2)
            {
                int PlayerID = args.Msg.whoAmI;
                OnPlayerDeath(PlayerID);
            }
        }

        void OnPlayerDeath(int PlayerID)
        {
            TSPlayer player = TShock.Players[PlayerID];
            Item emptyItem = new Item();
            // Go over the inventory
            for (int i = 0; i < 58; i++)
            {
                foreach (Item dropitem in DropItems)
                {
                    if (player.TPlayer.inventory[i].IsTheSameAs(dropitem))
                    {
                        // Spawn item
                        int itemIndex = Item.NewItem(new EntitySource_DebugCommand(), (int)player.X, (int)player.Y, player.TPlayer.width, player.TPlayer.height, dropitem.netID, player.TPlayer.inventory[i].stack, true, 0, true);
                        NetMessage.SendData((int)PacketTypes.ItemDrop, player.Index, -1, Terraria.Localization.NetworkText.FromFormattable(""), itemIndex);
                        // Empty slot
                        player.TPlayer.inventory[i] = emptyItem;
                        NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, Terraria.Localization.NetworkText.FromLiteral(player.TPlayer.inventory[i].Name), player.Index, i, player.TPlayer.inventory[i].prefix);
                        NetMessage.SendData((int)PacketTypes.PlayerSlot, player.Index, -1, Terraria.Localization.NetworkText.FromLiteral(player.TPlayer.inventory[i].Name), player.Index, i, player.TPlayer.inventory[i].prefix);
                    }
                }
            }
        }
    }
}