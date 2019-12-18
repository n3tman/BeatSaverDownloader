﻿using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace BeatSaverDownloader.UI.ViewControllers
{
    public class SongDescriptionViewController : BeatSaberMarkupLanguage.ViewControllers.BSMLResourceViewController
    {
        public override string ResourceName => "BeatSaverDownloader.UI.BSML.songDescription.bsml";
        [UIComponent("songDescription")]
        internal TextPageScrollView songDescription;
        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            base.DidDeactivate(deactivationType);
        }
        internal void ClearData()
        {
            if(songDescription)
            songDescription.SetText("");
        }
        internal void Initialize(BeatSaverSharp.Beatmap song)
        {
                songDescription.SetText(song.Description);
        }
        [UIAction("#post-parse")]
        internal void Setup()
        {

        }
    }
}
