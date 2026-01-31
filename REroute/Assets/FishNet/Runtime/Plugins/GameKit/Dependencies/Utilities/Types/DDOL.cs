using System;
using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types
{
    public class DDOL : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// Created instance of DDOL.
        /// </summary>
        private static DDOL _instance;
        #endregion


        private void Awake()
        {
            if (_instance != null) { Destroy(gameObject); }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Returns the current DDOL or creates one if not yet created.
        /// </summary>
        public static DDOL GetDDOL()
        {
            // Not yet made.
            if (_instance == null)
            {
                GameObject obj = new();
                obj.name = "DontDestroyOnLoad";
                DDOL ddol = obj.AddComponent<DDOL>();
                DontDestroyOnLoad(ddol);
                _instance = ddol;
                return ddol;
            }
            // Already  made.
            else
            {
                return _instance;
            }
        }
    }
}