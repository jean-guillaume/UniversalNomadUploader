﻿using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UniversalNomadUploader.Common;
using UniversalNomadUploader.DataModels.SQLModels;

namespace UniversalNomadUploader.SQLUtils
{
    public class SQLTableUtil
    {
        public static void CreateTables()
        {
            using (var db = new SQLiteConnection(GlobalVariables.dbPath))
            {
                db.CreateTable<SQLServer>();
                db.CreateTable<SQLUser>();
                db.CreateTable<SQLEvidence>();
                db.CreateTable<SQLEventLog>();
            }
        }
    }
}
