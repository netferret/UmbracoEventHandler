using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Publishing;
using Umbraco.Core.Services;

namespace xxx.Umbraco.Web.Utilities
{
    public class ItemEventHandler : ApplicationEventHandler
    {
        private static IContentService contentService
        {
            get { return ApplicationContext.Current.Services.ContentService; }
        }

        private static IEnumerable<string> AliasItemsPrefix
        {
            get
            {
                return new List<string> { "event", "news", "blog" };
            }
        }

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ContentService.Saved += ContentServiceSaved;
            ContentService.Published += ContentServicePublished;
        }

        /// <summary>
        /// Contents the service saved.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SaveEventArgs{IContent}"/> instance containing the event data.</param>
        private void ContentServiceSaved(IContentService sender, SaveEventArgs<IContent> e)
        {
            foreach (var aliasItemPrefix in AliasItemsPrefix)
            {
                var parentId = GetFolder(aliasItemPrefix);
                if (parentId == 0) continue;

                var yearFolder = GetYearFolderItem(parentId);
                var monthFolder = GetMonthFolderItemFromYear(yearFolder);
                var eventDay = GetDayFolderItemFromMonth(monthFolder);

                // Year Exists?
                if (yearFolder == null)
                {
                    yearFolder = contentService.CreateContent(DateTime.Now.Year.ToString(), parentId, "year", 0);
                    contentService.SaveAndPublishWithStatus(yearFolder);
                    return;
                }

                // Month Exists?
                if (monthFolder == null)
                {
                    monthFolder = contentService.CreateContent(DateTime.Now.Month.ToString(), yearFolder.Id, "month", 0);
                    contentService.SaveAndPublishWithStatus(monthFolder);
                    return;
                }

                // Day Exists
                if (eventDay == null)
                {
                    eventDay = contentService.CreateContent(DateTime.Now.Day.ToString(), monthFolder.Id, "day", 0);
                    contentService.SaveAndPublishWithStatus(eventDay);
                }
            }
        }

        /// <summary>
        /// Contents the service published.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="PublishEventArgs{IContent}"/> instance containing the event data.</param>
        private void ContentServicePublished(IPublishingStrategy sender, PublishEventArgs<IContent> e)
        {
            foreach (var aliasItemPrefix in AliasItemsPrefix)
            {
                var isAliasItem = CheckForAliasItem(e.PublishedEntities, aliasItemPrefix);
                if (!isAliasItem) return;

                var eventParentId = GetFolder(aliasItemPrefix);
                if (eventParentId == 0) return;

                var eventYear = GetYearFolderItem(eventParentId);
                var eventMonth = GetMonthFolderItemFromYear(eventYear);
                var eventDay = GetDayFolderItemFromMonth(eventMonth);

                // Get all Items withing the events folder.
                foreach (var item in e.PublishedEntities
                    .Where(x => x.ParentId != eventDay.Id && x.ContentType.Alias == aliasItemPrefix + "Item"))
                {
                    // Ensure there is a year and month folder to place the day in.
                    if (eventYear != null && eventMonth != null && eventDay.Id == 0)
                    {
                        var dayFolder = contentService.CreateContent(DateTime.Now.Day.ToString(), eventMonth.Id, "day", 0);
                        var success = contentService.SaveAndPublishWithStatus(dayFolder);

                        // ensure that we only move the event and not the other folders. 
                        if (success)
                            contentService.Move(item, dayFolder.Id);
                    }
                    else
                    {
                        contentService.Move(item, eventDay.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the event item alias.
        /// </summary>
        /// <param name="e">The e.</param>
        /// <returns></returns>
        private static bool CheckForAliasItem(IEnumerable<IContent> e, string aliasItem)
        {
            return e.Select(x => x.ContentType.Alias == aliasItem + "Item").Any();
        }

        private static int GetFolder(string aliasItem)
        {
            var homeId = contentService.GetRootContent().FirstOrDefault(x => x.Name == "Home").Id;
            var parentId = 0;

            if (contentService.GetById(homeId).Children().Any())
            {
                var folderItem = contentService.GetById(homeId).Children().FirstOrDefault(x => x.ContentType.Alias == aliasItem + "Folder");
                if (folderItem != null)
                    parentId = folderItem.Id;
            }
            return parentId;
        }

        private static IContent GetYearFolderItem(int parentId)
        {
            var eventYear = contentService.GetChildren(parentId).FirstOrDefault(x => x.Name == DateTime.Now.Year.ToString());
            return eventYear;
        }

        private static IContent GetMonthFolderItemFromYear(IContent eventYear)
        {
            IContent eventMonth = null;

            if (eventYear != null)
                if (eventYear.Children().Any())
                    eventMonth = eventYear.Children().FirstOrDefault(x => x.Name == DateTime.Now.Month.ToString());
            return eventMonth;
        }

        private static IContent GetDayFolderItemFromMonth(IContent eventMonth)
        {
            IContent eventDay = null;

            if (eventMonth != null)
                if (eventMonth.Children().Any())
                    eventDay = eventMonth.Children().FirstOrDefault(x => x.Name == DateTime.Now.Day.ToString());
            return eventDay;
        }

    }
}