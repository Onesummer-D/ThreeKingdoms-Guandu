using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType stage) {}
    }

    public static class PlayerPrefs
    {
        private static Dictionary<string,string> values=new Dictionary<string,string>();
        public static string GetString(string key,string fallback="") =>
            values.TryGetValue(key,out var val)?val:fallback;
        public static void SetString(string key,string value)=>values[key]=value;
        public static void Save(){}
    }
}
internal static class Program
{
    static int passed;
    static void Check(bool ok,string title) {
        if(!ok) throw new Exception("FAIL "+title);
        passed++; Console.WriteLine("PASS "+title);
    }
    static void Main()
    {
        Check(!PlayerIdentityState.HasId, "First launch requires an ID");
        Check(PlayerIdentityState.MinLength == 1 && PlayerIdentityState.MaxLength == 18,"Short session display ID limit");
        Check(!PlayerIdentityState.TrySave("",out _),"Blank name is rejected");
        Check(!PlayerIdentityState.TrySave("abcdefghijklmnopqrs",out _),"Overlong name is rejected");
        Check(!PlayerIdentityState.TrySave("<b>name</b>",out _),"TMP markup cannot be injected");
        Check(PlayerIdentityState.TrySave("含汉字",out _),"Chinese name accepted by the save validator");
        Check(PlayerIdentityState.CurrentId=="含汉字","Chinese name appears on poster in current session");
        Check(PlayerIdentityState.HasId,"Session unlocked after confirmation");

        PlayerIdentityState.ResetForNewLaunch();
        Check(!PlayerIdentityState.HasId,"New launch requires new confirmation");
        Check(PlayerIdentityState.CurrentId=="","Last session ID is cleared, not prefilled");
        Check(PlayerIdentityState.TrySave(" Guandu_07 ",out _),"Player can enter fresh Latin name");
        Check(PlayerIdentityState.CurrentId=="Guandu_07","New ID is trimmed");
        Check(!PlayerIdentityState.TrySave("too-long-invalid-display-name",out _) &&
              PlayerIdentityState.CurrentId=="Guandu_07",
            "Invalid later entry does not replace confirmed name");
        PlayerIdentityState.ResetForNewLaunch();
        Check(PlayerIdentityState.CurrentId=="","Second restart also clears the previous ID");
        Check(PlayerIdentityState.TrySave("111",out _),"Short numeric ID accepted");
        Console.WriteLine("player-id-tests: "+passed+"/"+passed);
    }
}
