﻿using Smod2.Commands;
using Smod2.EventHandlers;
using Smod2.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventManager
{
    internal sealed class EventHandler : ICommandHandler, IEventHandlerRoundStart, IEventHandlerRoundEnd
    {
        private PluginHandler Plugin { get; }
        public static Dictionary<string, IDictionary<string, string>> AllTranslations { set; get; }
        public static Dictionary<string, IDictionary<string, string>> AllConfigs { set; get; }
        private GameEvent NextEvent { set; get; }

        private List<GameEvent> Gamemodes;
        private bool eventOnGoing;
        private bool autoStopEvent;

        private EventHandler() { }
        internal EventHandler(PluginHandler plugin)
        {
            autoStopEvent = true;
            Plugin = plugin;
            Gamemodes = new List<GameEvent>();
        }



        public void RegisterCommand(GameEvent command)
        {
            if (Gamemodes.Find(x => x.GetName() == command.GetName() || command.GetCommands().Any(y => x.GetCommands().Contains(y))) == null)
            {
                Gamemodes.Add(command);
                command.Register();
                Plugin.Info($"Added {command.GetName()} event");
            }
            else
            {
                Plugin.Error($"Couldn't add {command.GetName()}");
            }
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            if (ev.Status == Smod2.API.ROUND_END_STATUS.ON_GOING)
                eventOnGoing = true;
            else
            {
                eventOnGoing = false;
                NextEvent.EventEnd(ev);
                Plugin.EventManager.RemoveEventHandlers(Plugin);
                if(autoStopEvent)
                    NextEvent = null;
                Plugin.AddEventHandlers(this);
            }
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            if (NextEvent != null)
            {
                NextEvent.EventStart(ev);
                if (NextEvent is IEventHandler)
                    Plugin.AddEventHandlers(NextEvent as IEventHandler);
            }
            else return;
            eventOnGoing = true;
        }

        public string[] OnCall(ICommandSender sender, string[] args)
        {
            if (args.Length == 0)
                return GameList();
            if (args.Length == 1)
                if (args[0] == "list")
                    return GameList();
            if (args.Length == 2)
                if(args[1] == "off")
                {
                    autoStopEvent = true;
                    return new string[] { $"Event is off" };
                }
            if(!autoStopEvent)
                return new string[] { $"Event {NextEvent.GetName()} is set to always on!" };
            if (eventOnGoing)
                return new string[] { "Event is currently on going, try after this round" };
            if (NextEvent != null)
                return new string[] { "Event is currently in queue, try another time" };
            string command = string.Empty;
            string arg = string.Empty;
            if (args.Length == 2)
                arg = args[1];
            if (arg == string.Empty)
                arg = "once";
            try
            {
                command = args[0];
                if (args.Length == 2)
                    arg = args[1].ToLower();
            }
            catch (Exception exp)
            {
                return new string[] { "Command is incorrect! ", exp.ToString(), "", "Try:", "- event <gamemode>", "- event <gamemode> <on/off/once>" };
            }

            GameEvent commandh = Gamemodes.Find(x => x.GetCommands().Contains(command));
            if (commandh != null)
            {
                autoStopEvent = !arg.Contains("on");
                if(NextEvent == null)
                    NextEvent = commandh;
            }
            else return new string[] { $"This event doesn't exist!" };
            return new string[] { $"[{commandh.GetName()}] Event is {arg}" };
        }


        public string GetUsage() => "event <gamemode> <on/off/once>";

        public string GetCommandDescription() => "Runs events/gamemodes";

        public IDictionary<string, IDictionary<string, string>> GetAllDefaultTranslations()
        {
            Dictionary<string, IDictionary<string, string>> translations = new();
            foreach (GameEvent gamemode in Gamemodes)
            {
                translations.Add(gamemode.GetName(), gamemode.DefaultTranslation);
            }
            return translations;
        }
        private string[] GameList()
        {
            List<string> list = new List<string>();
            list.Add("Avalible gamemodes: ");
            foreach(GameEvent gamemode in Gamemodes)
            {
                list.Add($"- {gamemode.GetName()} || {string.Join(",", gamemode.GetCommands())}");
            }
            return list.ToArray();
        }
    }
}
