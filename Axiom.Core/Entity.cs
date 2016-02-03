using System.Collections.Generic;
using System.Linq;


namespace Axiom.Core
{
    public class Entity
    {
        private static int _nextId = 1;

        private readonly List<Component> _components;
        private readonly List<Component> _componentsToAdd;

        private readonly Dictionary<string, object> _properties; 

        private bool _sortNeeded;
        public bool Alive { get; set; }

        public int Id { get; private set; }
        public Entity()
        {
            Id = _nextId;
            _nextId++;

            _components = new List<Component>();
            _componentsToAdd = new List<Component>();
            _properties = new Dictionary<string, object>();

            Alive = true;
        }

        public bool FireEvent(Event e)
        {
            foreach (var component in _components)
            {
               component.FireEvent(e);
            }
            return true;
        }

        public void Init()
        {
            if (_componentsToAdd.Any())
            {
                AddAttachedComponents();
                _componentsToAdd.Clear();
                _components.Sort(new ComponentComparer());
            }
        }

        public T GetProperty<T>(string name)
        {
            return _properties.ContainsKey(name) ? (T)_properties[name] : default(T);
        }

        public object SetProperty<T>(string name, T val)
        {
            if (_properties.ContainsKey(name))
                _properties[name] = val;
            else
                _properties.Add(name, val);

            return _properties[name];
        }


        public void Update(float dt)
        {
            foreach (var component in _components)
            {
                if (component.Alive)
                    component.Update(dt);
            }

            if (_componentsToAdd.Any())
            {
                AddAttachedComponents();
                _sortNeeded = true;
                _componentsToAdd.Clear();
            }

            if (_components.Any(x => !x.Alive))
            {
                RemoveAttachedComponents();
                _sortNeeded = true;
            }

            if (_sortNeeded)
                _components.Sort(new ComponentComparer());
        }

        public void AttachComponent(Component component)
        {
            _componentsToAdd.Add(component);
        }

        private void AddAttachedComponents()
        {
            foreach (var component in _componentsToAdd)
            {
                _components.Add(component);
            }
            foreach (var component in _components)
            {
                component.Init(this);
            }
        }

        private void RemoveAttachedComponents()
        {
            foreach (var component in _components.Where(x => !x.Alive))
            {
                _components.Remove(component);
            }
        }
    }
}
