using UnityEngine;

public class MathUtil
{
    /// <summary>Class for logarithmically lerping from 0 to 1. [<c>(a^t - 1) / (a - 1))</c>]</summary>
    public class LogLerper
    {
        /// <summary>Slope factor.</summary>
        public float a;
        /// <summary>Current free variable value.</summary>
        public float t;

        /// <summary>Class for logarithmically lerping from 0 to 1. [<c>(a^t - 1) / (a - 1))</c>]</summary>
        /// <param name="a">Slope factor.</param>
        public LogLerper(float a)
        {
            this.a = a; t = 0;
        }

        /// <summary>Get value at t: <c>y = (a^t - 1) / (a - 1))</c>]</summary>
        public float value(float t)
        {
            return ((Mathf.Pow(a, t) - 1) / (a - 1));
        }

        /// <summary>Move free variable and get resulting function value.</summary>
        /// <param name="delta">Amount to move free variable t.</param>
        public float move(float delta)
        {
            float new_t = Mathf.Clamp(t + delta, 0, 1);
            Debug.Log("Log.Lerper (" + t + " -> " + new_t + ") | dValue = " + (value(new_t) - value(t)));
            t = Mathf.Clamp(t + delta, 0, 1);
            return value(t);
        }
        /// <summary>Reset free variable t to 0.</summary>
        public void reset() { t = 0; }
    }
}



