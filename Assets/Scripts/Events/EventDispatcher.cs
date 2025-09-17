using System;
using System.Collections.Generic;

public static class EventDispatcher
{
    // ÿ���¼�����Ӧһ�� Delegate
    private static Dictionary<string, Delegate> eventTable = new Dictionary<string, Delegate>();

    /// <summary>
    /// ��Ӽ���
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
    /// �Ƴ�����
    /// </summary>
    public static void RemoveListener<T>(string eventName, Action<T> listener)
    {
        if (eventTable.ContainsKey(eventName))
        {
            eventTable[eventName] = (Action<T>)eventTable[eventName] - listener;
        }
    }

    /// <summary>
    /// �ɷ��¼�����������
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
    /// �ɷ��¼����޲�����
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
    /// ��Ӽ������޲�����
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
    /// �Ƴ��������޲�����
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