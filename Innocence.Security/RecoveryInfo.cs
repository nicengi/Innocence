using System;
using System.Collections.Generic;

namespace Innocence.Security
{
    [Serializable]
    public class RecoveryInfo
    {
        #region Fields

        #endregion

        #region Constructor
        public RecoveryInfo(Guid guid)
        {
            Guid = guid;
            Datas = new Dictionary<string, string>();
        }
        #endregion

        #region Properties
        public Guid Guid { get; }
        public string DirectoryName { get; set; }
        public string DirectoryRoot { get; set; }
        public Dictionary<string, string> Datas { get; }
        #endregion

        #region Methods
        public override string ToString()
        {
            return $"{DirectoryName}_{Guid:N}";
        }
        #endregion
    }
}