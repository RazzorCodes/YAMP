using RimWorld;
using System;
using System.Linq;
using Verse;

class Logger
{
    public static void Log(string type, string message)
    {
        Verse.Log.Message($"[YAMP:{type}]: {message}");
    }
}