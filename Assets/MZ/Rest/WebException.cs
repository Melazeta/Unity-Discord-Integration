using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MZ.Rest
{
    [Serializable]
    public class WebException : Exception
    {
        public WebException(string message) : base(message) { }
    }
}