using System;
using System.Collections.Generic;

public static class EventDispatcher
{
    // 每种事件名对应一个 Delegate
    private static Dictionary<string, Delegate> eventTable = new Dictionary<string, Delegate>();

    /// <summary>
    /// 添加监听
    /// </summary>
    public static void AddListener<T>(string eventName, Action<T> listener)
    {
        if (!eventTable.ContainsKey(eventName))
        {
            eventTable[eventName] = null;
        }

        eventTable[eventName] = (Action<T>)eventTable[eventName] + listener;
    }

    /// <summary>
    /// 移除监听
    /// </summary>
    public static void RemoveListener<T>(string eventName, Action<T> listener)
    {
        if (eventTable.ContainsKey(eventName))
        {
            eventTable[eventName] = (Action<T>)eventTable[eventName] - listener;
        }
    }

    /// <summary>
    /// 派发事件（单参数）
    /// </summary>
    public static void Dispatch<T>(string eventName, T arg)
    {
        if (eventTable.ContainsKey(eventName))
        {
            var callback = eventTable[eventName] as Action<T>;
            callback?.Invoke(arg);
        }
    }

    /// <summary>
    /// 派发事件（无参数）
    /// </summary>
    public static void Dispatch(string eventName)
    {
        if (eventTable.ContainsKey(eventName))
        {
            var callback = eventTable[eventName] as Action;
            callback?.Invoke();
        }
    }

    /// <summary>
    /// 添加监听（无参数）
    /// </summary>
    public static void AddListener(string eventName, Action listener)
    {
        if (!eventTable.ContainsKey(eventName))
        {
            eventTable[eventName] = null;
        }

        eventTable[eventName] = (Action)eventTable[eventName] + listener;
    }

    /// <summary>
    /// 移除监听（无参数）
    /// </summary>
    public static void RemoveListener(string eventName, Action listener)
    {
        if (eventTable.ContainsKey(eventName))
        {
            eventTable[eventName] = (Action)eventTable[eventName] - listener;
        }
    }
}


public class EventNames
{
    public static readonly string SELECT_PIECE = "SELECT_PIECE";
    public static readonly string DESELECT_PIECE = "DESELECT_PIECE";
    public static readonly string PUZZLE_COMPLETED = "PUZZLE_COMPLETED";
    public static readonly string PUZZLE_GENERATEION_DONE = "PUZZLE_GENERATEION_DONE";
    public static readonly string PUZZLE_GENERATEION_START = "PUZZLE_GENERATEION_START";
}