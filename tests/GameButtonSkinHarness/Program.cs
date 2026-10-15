using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Minimal Unity API stand-ins let CI compile and execute the production
// GameButtonSkin, including source-sprite resolution and semantic tones.
namespace UnityEngine
{
    public struct Color
    {
        public float r, g, b, a;
        public Color(float red, float green, float blue, float alpha = 1f)
        { r=red; g=green; b=blue; a=alpha; }
        public static Color white => new Color(1f,1f,1f,1f);
    }
    public struct Vector2
    {
        public float x,y;
        public Vector2(float a,float b){x=a;y=b;}
    }
    public struct Vector4
    {
        public float x,y,z,w;
        public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
        public float sqrMagnitude => x*x+y*y+z*z+w*w;
    }
    public class Sprite { public Vector4 border; }
    public class Component
    {
        public GameObject gameObject;
        public string name => gameObject.name;
        public T GetComponent<T>() where T:Component => gameObject.GetComponent<T>();
    }
    public class Transform : Component
    {
        public List<GameObject> children = new List<GameObject>();
        public Transform Find(string name) {
            foreach(var child in children)
                if(child.name == name)return child.transform;
            return null;
        }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component {
            List<T> results=new List<T>();
            foreach(var child in children) {
                T component = child.GetComponent<T>();
                if(component!=null)results.Add(component);
                results.AddRange(child.transform.GetComponentsInChildren<T>(includeInactive));
            }
            return results.ToArray();
        }
    }
    public class GameObject
    {
        public string name;
        private readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
        public Transform transform;
        public GameObject(string name) {
            this.name=name;
            transform=AddComponent<Transform>();
        }
        public T AddComponent<T>() where T:Component,new() {
            T instance=new T();instance.gameObject=this;components[typeof(T)]=instance;
            return instance;
        }
        public T GetComponent<T>() where T:Component {
            components.TryGetValue(typeof(T),out var component);
            return component as T;
        }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component {
            List<T> results=new List<T>();
            T own=GetComponent<T>();
            if(own!=null)results.Add(own);
            results.AddRange(transform.GetComponentsInChildren<T>(includeInactive));
            return results.ToArray();
        }
    }
}
namespace UnityEngine.UI
{
    public class Image : UnityEngine.Component
    {
        public enum Type { Simple, Sliced }
        public UnityEngine.Sprite sprite;
        public Type type;
        public UnityEngine.Color color;
        public bool raycastTarget, preserveAspect;
    }
    public class Selectable : UnityEngine.Component
    {
        public enum Transition { None, ColorTint }
    }
    public struct ColorBlock
    {
        public UnityEngine.Color normalColor, highlightedColor, selectedColor, pressedColor, disabledColor;
        public float colorMultiplier, fadeDuration;
    }
    public class Button : Selectable
    {
        public Image targetGraphic;
        public Transition transition;
        public ColorBlock colors;
    }
    public class Outline : UnityEngine.Component
    {
        public UnityEngine.Color effectColor;
        public UnityEngine.Vector2 effectDistance;
        public bool useGraphicAlpha;
    }
}

public class FinalUIManager
{
    public static FinalUIManager Instance {get;set;}
    public UnityEngine.GameObject startMenuPanel;
}

internal static class Program
{
    private static int checks;
    private static void Assert(bool truth,string what) {
        if(!truth)throw new Exception("FAIL "+what);
        checks++;
        Console.WriteLine("PASS "+what);
    }

    private static (Button,Image) MakeButton() {
        var go=new GameObject("Button");
        return (go.AddComponent<Button>(),go.AddComponent<Image>());
    }

    private static void Main()
    {
        var startMenu=new GameObject("StartMenu");
        var start=new GameObject("StartButton");
        startMenu.transform.children.Add(start);
        var startImage=start.AddComponent<Image>();
        var sourceSprite=new Sprite{border=new Vector4(16,16,16,16)};
        startImage.sprite=sourceSprite;
        start.AddComponent<Button>();
        FinalUIManager.Instance=new FinalUIManager{startMenuPanel=startMenu};

        var primary=MakeButton();
        GameButtonSkin.Apply(primary.Item1,primary.Item2,"采纳建议");
        Assert(ReferenceEquals(primary.Item2.sprite,sourceSprite),"Uses same decorative sprite as report/start menu");
        Assert(primary.Item2.type==Image.Type.Sliced,"Honors nine-slice borders");
        Assert(ReferenceEquals(primary.Item1.targetGraphic,primary.Item2),"Keeps image as button hit target");
        Assert(primary.Item1.transition==Selectable.Transition.ColorTint,"Provides interactive hover / press transition");
        Assert(primary.Item2.GetComponent<Outline>()!=null,"Ornate button has contrasting edge");

        var rejected=MakeButton();
        GameButtonSkin.Apply(rejected.Item1,rejected.Item2,"拒绝建议");
        Assert(rejected.Item2.color.r < primary.Item2.color.r,
            "Rejected suggestion uses subdued brick-red, not approval gold");
        Assert(rejected.Item2.color.r > rejected.Item2.color.g,
            "Rejection has a distinct warm-red hue");

        var withdrawn=MakeButton();
        GameButtonSkin.Apply(withdrawn.Item1,withdrawn.Item2,"撤销邀约");
        Assert(withdrawn.Item2.color.g > rejected.Item2.color.g,
            "Withdraw is visually distinct from rejection");
        var gallery=MakeButton();
        GameButtonSkin.Apply(gallery.Item1,gallery.Item2,"史官卡片");
        Assert(gallery.Item2.color.r > primary.Item2.color.r,
            "Historian gallery stands out more than default primary actions");
        Assert(gallery.Item2.GetComponent<Outline>().effectColor.a >
            primary.Item2.GetComponent<Outline>().effectColor.a,
            "Historian gallery has stronger gold border");

        var close=MakeButton();
        GameButtonSkin.Apply(close.Item1,close.Item2,"关闭");
        Assert(close.Item2.sprite==sourceSprite && close.Item2.color.r < primary.Item2.color.r,
            "Close uses matching style with quieter tone");

        // Normalised UI should not rely on Sprite borders always being nonzero.
        var flatSprite=new Sprite();
        var other=MakeButton();
        GameButtonSkin.Apply(other.Item1,other.Item2,GameButtonSkin.Tone.Secondary,flatSprite);
        Assert(other.Item2.type==Image.Type.Simple,"Non-sliced source never incorrectly forces sliced mode");

        FinalUIManager.Instance=null;
        var missing=MakeButton();
        GameButtonSkin.Apply(missing.Item1,missing.Item2,GameButtonSkin.Tone.Secondary);
        Assert(missing.Item2.type==Image.Type.Simple,"No scene art gracefully falls back to simple graphic");
        Assert(missing.Item2.GetComponent<Outline>()!=null,"Fallback still has visual edge");

        Console.WriteLine("game-button-skin-harness: "+checks+"/"+checks);
    }
}
