using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalNomadUploader.Common
{
    public class EvidenceConverter
    {
        public static IEnumerable<DataModels.FunctionalModels.FunctionnalEvidence> ToFunctionalEvidence(IEnumerable<DataModels.SQLModels.SQLEvidence> Evs)
        {
            List<DataModels.FunctionalModels.FunctionnalEvidence> e = new List<DataModels.FunctionalModels.FunctionnalEvidence>();
            foreach (var item in Evs)
            {
                e.Add(ToFunctionalEvidence(item));
            }
            return e;
        }

        public static DataModels.FunctionalModels.FunctionnalEvidence ToFunctionalEvidence(DataModels.SQLModels.SQLEvidence item)
        {
            return new DataModels.FunctionalModels.FunctionnalEvidence(item);
        }
    }
}
