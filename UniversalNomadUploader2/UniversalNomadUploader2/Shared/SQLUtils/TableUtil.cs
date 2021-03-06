﻿using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.FunctionalModels;
using UniversalNomadUploader.DataModels.SQLModels;

namespace UniversalNomadUploader.SQLUtils
{
    public class TableUtil
    {
        public static void CreateTables()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                db.CreateTable<Server>();
                db.CreateTable<UniversalNomadUploader.DataModels.SQLModels.User>();
                db.CreateTable<SQLEvidence>();
                db.CreateTable<EventLog>();
            }
        }
    }
}
