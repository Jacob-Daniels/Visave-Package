using System;
using UnityEngine;

/// <summary>
/// Payload allows for generic data Serialization & Deserialization using the JsonUtility library
/// </summary>
/// <remarks>
/// The Ipayload interface allows all payloads to be stored within a single list and accessed when the Serializer is saving all the data within a SaveData instance.
/// </remarks>

namespace Visave
{
    public interface IPayload
    {
        public object m_basePayload { get; set; }
        public string SerializeObject();
    }

    [Serializable]
    public class Payload<T> : IPayload
    {
        public object m_basePayload { get; set; }
        public T m_payload;
        public Payload(T payload)
        {
            m_basePayload = payload;
            m_payload = (T)payload;
        }

        public string SerializeObject()
        {
            return JsonUtility.ToJson(this);
        }
        public Payload<T> DeserializeObject(string jsonData)
        {
            return JsonUtility.FromJson<Payload<T>>(jsonData);
        }
    }
}