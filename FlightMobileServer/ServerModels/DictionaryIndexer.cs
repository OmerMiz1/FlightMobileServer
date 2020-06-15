using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlightMobileServer.ServerModels {
    public class DictionaryIndexer : INotifyPropertyChanged{
        public event PropertyChangedEventHandler PropertyChanged;

        private Dictionary<string, string> dic = new Dictionary<string, string>();
        public string this[string key] {
            get => dic[key];
            set {
                if (key != null)
                {
                    dic[key] = value;
                }
            }
        }
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public IDictionaryEnumerator GetEnumerator()
        {
            return ((IDictionary) dic).GetEnumerator();
        }
        public bool ContainsKey(string key)
        {
            return dic.ContainsKey(key);
        }
    }
}