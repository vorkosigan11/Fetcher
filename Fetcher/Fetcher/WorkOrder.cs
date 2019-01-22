using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fetcher
{
    public class WorkOrder
    {
        private string clientName;
        private string clientOrder;
        private string remark;
        private DateTime workOrderDate;
        private string workOrderNumber;

        public string GetWorkOrderNumber()
        {
            if (workOrderNumber != null)
            {
                return workOrderNumber.Split(new char[] { '/' }).ElementAt(0).ToString();
            }
            else
            {
                return "";
            }
        }

        public string GetWorkOrderYear()
        {
            if (workOrderNumber != null)
            {
                return workOrderNumber.Split(new char[] { '/' }).ElementAt(2).ToString();
            }
            else
            {
                return "";
            }
        }

        public string ClientOrder
        {
            get { return clientOrder; }
            set { clientOrder = value; }
        }

        public string ClientName
        {
            get { return clientName; }
            set { clientName = value; }
        }

        public string Remark
        {
            get { return remark; }
            set { remark = value; }
        }

        public DateTime WorkOrderDate
        {
            get { return workOrderDate; }
            set { workOrderDate = value; }
        }

        public string WorkOrderNumber
        {
            get { return workOrderNumber; }
            set { workOrderNumber = value; }
        }
    }
}