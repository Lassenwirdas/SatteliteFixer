using System;
using UnityEngine;

namespace SatelliteFixer.Session
{
    public enum AppRole
    {
        /// <summary>PC app: mission control, network host.</summary>
        Operator,

        /// <summary>VR app on the Quest: network client.</summary>
        Astronaut,
    }

    public enum RoleOverride
    {
        /// <summary>Role follows the platform (and Multiplayer Play Mode in the editor).</summary>
        Automatic,
        Operator,
        Astronaut,
    }

    /// <summary>
    /// Decides which role this app instance plays (decision 7: the role follows the platform).
    /// </summary>
    public static class RoleResolver
    {
        /// <summary>Command line argument to force a role in builds, e.g. <c>-sfRole astronaut</c>.</summary>
        public const string RoleArgument = "-sfRole";

        public static AppRole Resolve(RoleOverride editorOverride)
        {
            var argument = CommandLine.GetValue(RoleArgument);
            if (!string.IsNullOrEmpty(argument) && Enum.TryParse(argument, true, out AppRole fromArgument))
                return fromArgument;

#if UNITY_EDITOR
            if (editorOverride == RoleOverride.Operator)
                return AppRole.Operator;
            if (editorOverride == RoleOverride.Astronaut)
                return AppRole.Astronaut;

            // With Multiplayer Play Mode the main editor hosts and every virtual player is an astronaut.
            return Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor ? AppRole.Operator : AppRole.Astronaut;
#else
            return Application.platform == RuntimePlatform.Android ? AppRole.Astronaut : AppRole.Operator;
#endif
        }
    }

    /// <summary>
    /// Small helper to read <c>-name value</c> pairs from the command line.
    /// </summary>
    public static class CommandLine
    {
        public static string GetValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }

        public static bool HasFlag(string name)
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
