﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fujitsu.eDoc.BOM
{
    public class FuIndsendelseType
    {
        public BOM.BOMSagsbehandling.ServiceMaalStatistikType ServiceMaalStatistikType { get; set; }
        public BOMSagsbehandling.IndsendelseType IndsendelseType { get; set; }



        public FuIndsendelseType(BOM.BOMSagsbehandling.ServiceMaalStatistikType serviceMaalStatistikType, BOMSagsbehandling.IndsendelseType indsendelseType)
        {
            this.ServiceMaalStatistikType = serviceMaalStatistikType;
            this.IndsendelseType = indsendelseType;
        }

    }
}
