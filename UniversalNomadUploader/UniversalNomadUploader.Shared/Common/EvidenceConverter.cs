using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.Common
{
    public class EvidenceConverter
    {
        public static IEnumerable<DataModels.FunctionalModels.Evidence> ToFunctionalEvidence(IEnumerable<DataModels.SQLModels.Evidence> Evs)
        {
            List<DataModels.FunctionalModels.Evidence> e = new List<DataModels.FunctionalModels.Evidence>();
            foreach (var item in Evs)
            {
                e.Add(ToFunctionalEvidence(item));
            }
            return e;
        }

        public static DataModels.FunctionalModels.Evidence ToFunctionalEvidence(DataModels.SQLModels.Evidence item)
        {
            return new DataModels.FunctionalModels.Evidence(item);
        }
    }
}
