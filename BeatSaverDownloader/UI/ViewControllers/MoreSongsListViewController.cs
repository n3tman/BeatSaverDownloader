﻿using HMUI;
using IPA.Utilities;
using System.Linq;
using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using System;
namespace BeatSaverDownloader.UI.ViewControllers
{
    public class MoreSongsListViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.moreSongsList.bsml";
        [UIComponent("list")]
        public CustomListTableData customListTableData;
        public List<BeatSaverSharp.Beatmap> _songs = new List<BeatSaverSharp.Beatmap>();
        public LoadingControl loadingSpinner;
        public bool Working
        {
            get { return _working; }
            set { _working = value; if (!loadingSpinner) return; SetLoading(value); }
        }
        private bool _working;
        uint lastPage = 0;


        [UIAction("listSelect")]
        internal void Select(TableView tableView, int row)
        {
            MoreSongsFlowCoordinator.didSelectSong.Invoke(_songs[row], customListTableData.data[row].icon);
        }
        [UIAction("pageDownPressed")]
        internal void PageDownPressed()
        {
            //Plugin.log.Info($"Number of cells {7}  visible cell last idx {customListTableData.tableView.visibleCells.Last().idx}  count {customListTableData.data.Count()}   math {customListTableData.data.Count() - customListTableData.tableView.visibleCells.Last().idx})");
            if (!(customListTableData.data.Count >= 1)) return;
            if ((customListTableData.data.Count() - customListTableData.tableView.visibleCells.Last().idx) <= 14)
            {
                GetNewPage();
            }


        }
        internal async void GetNewPage(uint count = 1)
        {
            Plugin.log.Info($"Fetching {count} new page(s)");
            Working = true;
            for (uint i = 0; i < count; ++i)
            {
                var songs = await BeatSaverSharp.BeatSaver.Latest(lastPage);
                lastPage++;
                _songs.AddRange(songs.Docs);
                foreach (var song in songs.Docs)
                {
                    byte[] image = await song.FetchCoverImage();
                    Texture2D icon = Misc.Sprites.LoadTextureRaw(image);
                    customListTableData.data.Add(new CustomListTableData.CustomCellInfo(song.Name, song.Uploader.Username, icon));
                }
                customListTableData.tableView.ReloadData();
            }

            Working = false;
        }
        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);
        }

        [UIAction("#post-parse")]
        internal async void SetupList()
        {

            (transform as RectTransform).sizeDelta = new Vector2(70, 0);
            (transform as RectTransform).anchorMin = new Vector2(0.5f, 0);
            (transform as RectTransform).anchorMax = new Vector2(0.5f, 1);

            loadingSpinner = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<LoadingControl>().First(), gameObject.transform);
            customListTableData.data.Clear();
            // Add items here
            GetNewPage(2);
            // customListTableData.tableView.ScrollToCellWithIdx(InitialItem, HMUI.TableViewScroller.ScrollPositionType.Beginning, false);
            // customListTableData.tableView.SelectCellWithIdx(InitialItem);

        }

        public void SetLoading(bool value)
        {
            if(value)
            {
                loadingSpinner.gameObject.SetActive(true);
                loadingSpinner.ShowLoading();
            }
            else
            {
                loadingSpinner.gameObject.SetActive(false);
            }
        }
    }
}

