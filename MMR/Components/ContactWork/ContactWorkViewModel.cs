using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using MMR.Data;
using MMR.Models;
using MMR.ViewModels;

namespace MMR.Components.ContactWork;

public partial class ContactWorkViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<WorkContact> _workContacts;

    public ContactWorkViewModel()
    {
    }

    public void GetWorks(int contactId)
    {
        var contactWorks = DbHelper.Db.WorkContacts.AsNoTracking().Where(wt => wt.ContactId == contactId)
            .Include(wt => wt.Work).ToList();
        if (contactWorks.Any())
        {
            WorkContacts = new ObservableCollection<WorkContact>(contactWorks);
        }
    }
}