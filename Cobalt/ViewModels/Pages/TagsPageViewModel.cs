﻿using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Cobalt.Common.Analysis;
using Cobalt.Common.Data;
using Cobalt.Common.Data.Repository;
using Cobalt.Common.IoC;
using Cobalt.Common.UI.ViewModels;
using Cobalt.ViewModels.Utils;
using Cobalt.Views.Dialogs;

namespace Cobalt.ViewModels.Pages
{
    public class TagsPageViewModel : PageViewModel
    {

        public TagsPageViewModel(IResourceScope scope, IEntityStreamService entities, IDbRepository repo,
            INavigationService svc) : base(scope)
        {
            NavigationService = svc;
            Repository = repo;
            EntityStreamService = entities;
        }

        public static Func<double, string> HourFormatter => x => x / 600000000 + "min";
        public static Func<double, string> DayFormatter => x => x == 0 ? "" : x / 36000000000 + "h";
        public static Func<double, string> DayHourFormatter => x => (x % 12 == 0 ? 12 : x % 12) + (x >= 12 ? "p" : "a");
        public static Func<double, string> DayOfWeekFormatter => x => ((DayOfWeek) (int) x).ToString();

        public ObservableCollection<TagViewModel> Tags { get; set; }

        public INavigationService NavigationService { get; set; }

        public IEntityStreamService EntityStreamService { get; set; }

        public IDbRepository Repository { get; set; }


        protected override void OnActivate(IResourceScope res)
        {
            Tags = new ObservableCollection<TagViewModel>();
            Repository.GetTags()
                .Select(x => new TagViewModel(x))
                .Subscribe(x => Tags.Add(x));
        }

        protected override void OnDeactivate(bool close, IResourceScope resources)
        {
            Tags = null;
        }

        public void SelectTag(TagViewModel tag)
        {
        }

        public void AddTag(string tagName)
        {
            var tag = new Tag {Name = tagName};
            Repository.AddTag(tag);
            Tags.Add(new TagViewModel(tag));
        }

        public void DeleteTag(TagViewModel tag)
        {
            Repository.RemoveTag((Tag) tag.Entity);
            Tags.Remove(tag);
        }

        public async void AddAppsToTag(TagViewModel tag)
        {
            var apps = EntityStreamService.GetApps().Select(x => new AppViewModel(x));
            var result = (IList) await NavigationService.ShowDialog<SelectAppsDialog>(apps, Resources);
            if (result == null) return;
            foreach (var appViewModel in result) AddTagToApp(tag, (AppViewModel) appViewModel);
        }

        public void AddTagToApp(TagViewModel tag, AppViewModel app)
        {
            Repository.AddTagToApp((Tag) tag.Entity, (App) app.Entity);
            tag.TaggedApps.Add(app);
        }

        public void RemoveTagFromApp(TagViewModel tag, AppViewModel app)
        {
            Repository.RemoveTagFromApp((Tag) tag.Entity, (App) app.Entity);
            tag.TaggedApps.Remove(app);
        }
    }
}