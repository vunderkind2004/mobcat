﻿using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.MobCAT.MVVM;
using News.Helpers;
using News.Models;
using Xamarin.Essentials;

namespace News.ViewModels
{
    /// <summary>
    /// News by category view model.
    /// </summary>
    public class NewsByCategoryViewModel : BaseNavigationViewModel
    {
        private int _selectedCategoryPosition;
        private bool _isSelectNextCategoryTipEnabled;

        public CategoryNewsViewModel SelectedCategory => Categories.Count > _selectedCategoryPosition ? Categories[_selectedCategoryPosition] : null;

        public int SelectedCategoryPosition
        {
            get { return _selectedCategoryPosition; }
            set
            {
                if (RaiseAndUpdate(ref _selectedCategoryPosition, value))
                {
                    Raise(nameof(SelectedCategory));
                    OnSelectedCategoryPositionChanged();
                }
            }
        }

        public ObservableCollection<CategoryNewsViewModel> Categories { get; } = new ObservableCollection<CategoryNewsViewModel>
        {
            new CategoryNewsViewModel("Top News \ud83d\udd25"),
            new CategoryNewsViewModel("Business \ud83d\udcbc", News.Models.Categories.Business),
            new CategoryNewsViewModel("Entertainment \ud83d\udc83\ud83d\udd7a", News.Models.Categories.Entertainment),
            new CategoryNewsViewModel("Health \ud83e\uddec", News.Models.Categories.Health),
            new CategoryNewsViewModel("Science \ud83d\udd2c", News.Models.Categories.Science),
            new CategoryNewsViewModel("Sports \ud83e\udd3c", News.Models.Categories.Sports),
            new CategoryNewsViewModel("Technology \ud83d\udc69‍\ud83d\udcbb" ,News.Models.Categories.Technology),
        };

        public bool IsSelectNextCategoryTipEnabled
        {
            get { return _isSelectNextCategoryTipEnabled; }
            set { RaiseAndUpdate(ref _isSelectNextCategoryTipEnabled, value); }
        }

        public bool IsSelectNextCategoryTipNotRequired
        {
            get { return Preferences.Get(nameof(IsSelectNextCategoryTipNotRequired), false); }
            set
            {
                if (IsSelectNextCategoryTipNotRequired != value)
                {
                    Preferences.Set(nameof(IsSelectNextCategoryTipNotRequired), value);
                    Raise(nameof(IsSelectNextCategoryTipNotRequired));
                }
            }
        }

        public Command SelectNextCategoryCommand { get; }

        public NewsByCategoryViewModel()
        {
            SelectNextCategoryCommand = new Command(OnSelectNextCategoryCommandExecuted);
        }

        public async override Task InitAsync()
        {
            ShowSelectNextCategoryTipIfRequiredAsync().HandleResult();
            await SelectedCategory.InitAsync();
            await base.InitAsync();
        }

        private void OnSelectedCategoryPositionChanged()
        {
            System.Diagnostics.Debug.WriteLine($"SelectedCategoryPosition changed to {SelectedCategoryPosition}");
            IsSelectNextCategoryTipEnabled = false;
            IsSelectNextCategoryTipNotRequired = true;
            SelectedCategory.InitAsync().HandleResult();
        }

        private async Task ShowSelectNextCategoryTipIfRequiredAsync()
        {
            if (IsSelectNextCategoryTipNotRequired)
                return;

            IsSelectNextCategoryTipEnabled = false;
            await Task.Delay(10000);

            // Verify again if a user took an action and changed a category
            if (IsSelectNextCategoryTipNotRequired)
                return;

            IsSelectNextCategoryTipEnabled = true;
        }

        private void OnSelectNextCategoryCommandExecuted()
        {
            var nextPosition = SelectedCategoryPosition + 1;
            if (nextPosition >= Categories.Count || nextPosition < 0)
                nextPosition = 0;

            SelectedCategoryPosition = nextPosition;
        }
    }
}