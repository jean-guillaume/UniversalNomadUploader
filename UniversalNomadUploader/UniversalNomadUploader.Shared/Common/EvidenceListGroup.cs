using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniversalNomadUploader
{
    public class EvidenceListGroup<T> : List<object>
    {
        /// <summary>
        /// Key that represents the group of objects and used as group header.
        /// </summary>
        public object Key { get; set; }

        public new IEnumerator<object> GetEnumerator()
        {
            return (System.Collections.Generic.IEnumerator<object>)base.GetEnumerator();
        }
    }
}
