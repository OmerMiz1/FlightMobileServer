using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;

namespace FlightMobileServer.ServerModels {
    /*** Taken from: https://stackoverflow.com/questions/20532620/c-sharp-wpf-binding-to-indexed-property-what-am-i-doing-wrong
    *    And: https://stackoverflow.com/questions/9392956/how-to-make-binding-to-dictionary-item-with-change-notification/9393016#9393016
    ***/
    public class DictionaryProperty : INotifyCollectionChanged {
        private Dictionary<string, string> dic { get; set; }

        public string this[string key] {
            get => dic[key];
            set {
                if (key != null && dic[key] != value) {
                    dic[key] = value;
                }

                OnPropertyChanged("Item[" + key + "]");
            }
        }
        internal DictionaryProperty() {
            m_data = new ObservableCollection<string>();
            m_data.CollectionChanged += (s, e) => {
                CollectionChanged?.Invoke(s, e);
            };
        }

        public string this[string key] {
            get {
                if (key != null) {
                    return m_data[key];
                }
                if (m_data.Count > key) {
                    return m_data[key];
                } else {
                    return "Element not set for " + key.ToString();
                }
            }
            set {
                if (m_data.Count > key) {
                    m_data[key] = value;
                } else {
                    m_data.Insert(key, value);
                }
                Console.WriteLine(value);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}