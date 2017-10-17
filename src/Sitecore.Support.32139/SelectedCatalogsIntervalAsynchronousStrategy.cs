using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommerceServer.Core.Catalog;
using Sitecore.Commerce.Connect.CommerceServer;
using Sitecore.Commerce.Connect.CommerceServer.Caching;
using Sitecore.Commerce.Connect.CommerceServer.Catalog;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data;
using LegacyCommerceServer = CommerceServer;

namespace Sitecore.Support.Commerce.Connect.CommerceServer.Search
{
    public class SelectedCatalogsIntervalAsynchronousStrategy: Sitecore.Commerce.Connect.CommerceServer.Search.SelectedCatalogsIntervalAsynchronousStrategy
    {
        private void LogMessage(string format, params object[] args)
        {
            string str = string.Format(CultureInfo.InvariantCulture, "[Index={0}] {1} : ", new object[]
            {
                this.Index.Name,
                base.GetType().Name
            });
            string str2 = format;
            if (args != null || args.Length != 0)
            {
                str2 = string.Format(CultureInfo.InvariantCulture, format, args);
            }
            CrawlingLog.Log.Info(str + str2, null);
        }

        private void EnsureCacheCoherency(List<ExternalIdInformation> externalInformation)
        {
            if (externalInformation != null && externalInformation.Count > 0)
            {
                var maxLastModified = externalInformation.Max(i => i.LastModified).ToUniversalTime();
                if (!CacheRefresh.LastCatalogCacheRefreshTimeUtc.HasValue ||
                    maxLastModified > CacheRefresh.LastCatalogCacheRefreshTimeUtc)
                {
                    CatalogUtility.RefreshCatalogCache(ID.Null);
                }

                externalInformation.ForEach(i => CommerceUtility.RemoveItemFromSitecoreCaches(new ID(i.ExternalId), this.Database.Name));
            }
        }
        private void IndexIncrementalUpdate(List<Guid> indexableUniqueIds)
        {
            if (indexableUniqueIds != null && indexableUniqueIds.Count > 0)
            {
                var updatedInformation =
                    indexableUniqueIds.Select(x => this.CatalogRepository.GetExternalIdInformation(x)).ToList();
                this.EnsureCacheCoherency(updatedInformation);
                IndexCustodian.IncrementalUpdate(this.Index, indexableUniqueIds.Select(x => new SitecoreItemUniqueId(new ItemUri(new ID(x), this.Database))));
            }
        }

        private Guid GetParentCategoryId(string catalogName, string categoryName)
        {
            string language = null;
            var contextManager = Sitecore.Commerce.Connect.CommerceServer.CommerceTypeLoader.CreateInstance<Sitecore.Commerce.Connect.CommerceServer.ICommerceServerContextManager>();
            var categoryConfiguration = new LegacyCommerceServer.Core.Catalog.CategoryConfiguration()
            {
                LoadChildProducts = false,
                LoadChildCategories = false,
                RecursiveChildProducts = false,
                RecursiveChildCategories = false,
                LoadRelatedProducts = false,
                LoadRelatedCategories = false,
                LoadAncestorCategories = true,
                LoadFromCache = false
            };
            var category = contextManager.CatalogContext.GetCategory(catalogName, categoryName, language, categoryConfiguration);
            return category.AncestorCategories.ToList()[0].ExternalId;
        }

        private List<Guid> GetParentCategoryIds(string catalogName, List<ExternalIdInformation> catalogItemExternalIdInformation)
        {
            List<Guid> uniqueIds = new List<Guid>();
            foreach (var info in catalogItemExternalIdInformation)
            {
                var parentCategoryId = this.GetParentCategoryId(catalogName, info.CategoryName);
                uniqueIds.Add(parentCategoryId);
            }
            return uniqueIds;
        }

        public SelectedCatalogsIntervalAsynchronousStrategy(string database, string rootPath, string interval = null) : base(database, rootPath, interval)
		{
        }

        protected override void IndexModifiedCatalogItems(string catalogName)
        {
            base.IndexModifiedCatalogItems(catalogName);
            List<Guid> indexableUniqueIds = new List<Guid>();
            List<ExternalIdInformation> deletedCatalogItemExternalIdInformation = this.CatalogRepository.GetDeletedCatalogItemExternalIdInformation(catalogName, this.GetMinCatalogItemLastModifiedDate()) ?? new List<ExternalIdInformation>();
            List<ExternalIdInformation> modifiedCatalogItemExternalIdInformation =
                this.CatalogRepository.GetModifiedCatalogItemExternalIdInformation(this.GetMinCatalogItemLastModifiedDate(), catalogName, this.GetItemTypesToIndex());
            indexableUniqueIds = this.GetParentCategoryIds(catalogName, deletedCatalogItemExternalIdInformation);
            indexableUniqueIds.AddRange(this.GetParentCategoryIds(catalogName, modifiedCatalogItemExternalIdInformation));
            this.LogMessage("Updating {0} parent catalog items.", new object[]
            {
                indexableUniqueIds.Count
            });

            this.IndexIncrementalUpdate(indexableUniqueIds);
        }
    }
}
