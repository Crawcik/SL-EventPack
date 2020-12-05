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
        private GameEvent NextEvent { set; get; }

        private List<GameEvent> Commands;
        private bool eventOnGoing;
        private bool autoStopEvent;

        private EventHandler() { }
        internal EventHandler(PluginHandler plugin)
        {
            Plugin = plugin;
            Commands = new List<GameEvent>();
        }



        public void RegisterCommand(GameEvent command)
        {
            if (Commands.Find(x => x.GetName() == command.GetName() || command.GetCommands().Any(y => x.GetCommands().Contains(y))) == null)
            {
                Commands.Add(command);
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
            if (eventOnGoing)
            {
                return new string[] { "Event is currently on going, try after this round" };
            }
            if (NextEvent != null)
            {
                return new string[] { "Event is currently in queue, try another time" };
            }

            string command = "";
            string arg = "";

            try
            {
                command = args[0];
                if (arg.Length == 2)
                    arg = args[1];
            }
            catch (Exception exp)
            {
                return new string[] { "Command is incorrect! ", exp.ToString(), "","Try:", "- event <gamemode>", "- event <gamemode> <on/off/once>" };
            }
            if (arg == "")
                arg = "once";

            GameEvent commandh = Commands.Find(x => x.GetCommands().Contains(command));
            if (commandh != null)
            {
                autoStopEvent = arg != "on";
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
            foreach (GameEvent sl_event in Commands)
            {
                translations.Add(sl_event.GetName(), sl_event.DefaultTranslation);
            }
            return translations;
        }
    }
}
