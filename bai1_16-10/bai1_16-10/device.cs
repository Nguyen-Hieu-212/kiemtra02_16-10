using System.ComponentModel;

namespace DeviceManager
{
    // Lớp thiết bị, hỗ trợ INotifyPropertyChanged để Grid tự cập nhật khi thuộc tính đổi
    public class Device : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _category;
        private decimal _price;
        private int _quantity;

        public string Id { get => _id; set { _id = value; Notify(nameof(Id)); } }
        public string Name { get => _name; set { _name = value; Notify(nameof(Name)); } }
        public string Category { get => _category; set { _category = value; Notify(nameof(Category)); } }
        public decimal Price { get => _price; set { _price = value; Notify(nameof(Price)); } }
        public int Quantity { get => _quantity; set { _quantity = value; Notify(nameof(Quantity)); } }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}