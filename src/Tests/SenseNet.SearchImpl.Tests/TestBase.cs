﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search.Indexing;
using SenseNet.SearchImpl.Tests.Implementations;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.Messaging;

namespace SenseNet.SearchImpl.Tests
{
    public class TestBase
    {
        protected T Test<T>(Func<T> callback)
        {
            TypeHandler.Initialize(new Dictionary<Type, Type[]>
            {
                {typeof(ElevatedModificationVisibilityRule), new[] {typeof(SnElevatedModificationVisibilityRule)}}
            });

            var dataProvider = new InMemoryDataProvider();
            StartSecurity(dataProvider);

            DistributedApplication.Cache.Reset();

            using (new Tools.SearchEngineSwindler(new TestSearchEngine()))
            //using (Tools.Swindle(typeof(IndexManager), "_indexingEngineFactory", new InMemoryIndexingEngineFactory()))
            //using (Tools.Swindle(typeof(StorageContext.Search), "ContentRepository", new TestSearchEngineSupport(DefaultIndexingInfo)))
            using (Tools.Swindle(typeof(StorageContext.Search), "ContentRepository", new SearchEngineSupport()))
            using (Tools.Swindle(typeof(AccessProvider), "_current", new DesktopAccessProvider()))
            using (Tools.Swindle(typeof(DataProvider), "_current", dataProvider))
            using (new SystemAccount())
            {
                IndexManager.Start(new InMemoryIndexingEngineFactory(), TextWriter.Null);
                return callback();
            }
        }
        private void StartSecurity(InMemoryDataProvider repo)
        {
            var securityDataProvider = new MemoryDataProvider(new DatabaseStorage
            {
                Aces = new List<StoredAce>
                {
                    new StoredAce {EntityId = 2, IdentityId = 1, LocalOnly = false, AllowBits = 0x0EF, DenyBits = 0x000}
                },
                Entities = repo.GetSecurityEntities().ToDictionary(e => e.Id, e => e),
                Memberships = new List<Membership>
                {
                    new Membership
                    {
                        GroupId = Identifiers.AdministratorsGroupId,
                        MemberId = Identifiers.AdministratorUserId,
                        IsUser = true
                    }
                },
                Messages = new List<Tuple<int, DateTime, byte[]>>()
            });

            SecurityHandler.StartSecurity(false, securityDataProvider, new DefaultMessageProvider());
        }

    }
}
