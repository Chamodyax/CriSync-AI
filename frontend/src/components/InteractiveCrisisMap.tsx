import React, { useEffect, useRef } from 'react';
import L from 'leaflet';

interface MapProps {
  requests: any[];
  shelters: any[];
  resources: any[];
  operationalAreas: any[];
  onSelectLocation?: (lat: number, lon: number) => void;
}

export const InteractiveCrisisMap: React.FC<MapProps> = ({
  requests,
  shelters,
  resources,
  operationalAreas,
  onSelectLocation
}) => {
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const layerGroupRef = useRef<L.LayerGroup | null>(null);

  useEffect(() => {
    if (!mapContainerRef.current || mapInstanceRef.current) {
      return;
    }

    const map = L.map(mapContainerRef.current).setView([6.6828, 80.3992], 10);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap Humanitarian Contributors'
    }).addTo(map);

    const group = L.layerGroup().addTo(map);
    mapInstanceRef.current = map;
    layerGroupRef.current = group;

    map.on('click', (evt: L.LeafletMouseEvent) => {
      if (onSelectLocation) {
        onSelectLocation(
          Number(evt.latlng.lat.toFixed(4)),
          Number(evt.latlng.lng.toFixed(4))
        );
      }
    });

    setTimeout(() => {
      map.invalidateSize();
    }, 150);

    return () => {
      map.remove();
      mapInstanceRef.current = null;
    };
  }, []);

  useEffect(() => {
    const group = layerGroupRef.current;
    if (!group) {
      return;
    }

    group.clearLayers();

    operationalAreas.forEach((area) => {
      if (area.riskLevel === 'Critical') {
        L.marker([area.latitude, area.longitude], {
          interactive: false,
          icon: L.divIcon({
            className: 'crisync-map-sonar-icon',
            html: '<span class="crisync-sonar-ring"></span><span class="crisync-sonar-ring crisync-sonar-delay"></span>',
            iconSize: [44, 44],
            iconAnchor: [22, 22]
          })
        }).addTo(group);
      }
      L.circle([area.latitude, area.longitude], {
        radius: (area.coverageRadiusKm || 10) * 1000,
        color: area.riskLevel === 'Critical' ? '#f43f5e' : '#f59e0b',
        fillColor: area.riskLevel === 'Critical' ? '#f43f5e' : '#f59e0b',
        fillOpacity: 0.14,
        weight: 2
      })
        .bindPopup(
          `<strong>${area.zoneName}</strong><br/>District: ${area.district}<br/>Risk: ${area.riskLevel}<br/>Est. Affected: ${area.estimatedAffectedPopulation}`
        )
        .addTo(group);
    });

    requests.forEach((req) => {
      const isUrgent =
        req.finalApprovedUrgency === 'LifeThreatening' || req.finalApprovedUrgency === 'Critical';
      const color = isUrgent
        ? '#f43f5e'
        : req.finalApprovedUrgency === 'High'
        ? '#fb923c'
        : '#6366f1';

      if (isUrgent) {
        L.marker([req.latitude, req.longitude], {
          interactive: false,
          icon: L.divIcon({
            className: 'crisync-map-sonar-icon',
            html: '<span class="crisync-sonar-ring"></span><span class="crisync-sonar-ring crisync-sonar-delay"></span>',
            iconSize: [44, 44],
            iconAnchor: [22, 22]
          })
        }).addTo(group);
      }

      L.circleMarker([req.latitude, req.longitude], {
        radius: 9,
        fillColor: color,
        color: '#ffffff',
        weight: 2,
        fillOpacity: 0.95
      })
        .bindPopup(
          `<strong>[${req.referenceCode}] ${req.title}</strong><br/>Category: ${req.category}<br/>Urgency: ${req.finalApprovedUrgency} (Score: ${req.priorityScore})<br/>Status: ${req.status}<br/>Affected: ${req.affectedPeopleCount} people`
        )
        .addTo(group);
    });

    shelters.forEach((sh) => {
      L.circleMarker([sh.latitude, sh.longitude], {
        radius: 10,
        fillColor: '#10b981',
        color: '#ecfdf5',
        weight: 2,
        fillOpacity: 0.95
      })
        .bindPopup(
          `<strong>Shelter: ${sh.name}</strong><br/>Occupancy: ${sh.currentOccupancy} / ${sh.totalCapacity}<br/>Wheelchair Accessible: ${sh.isWheelchairAccessible ? 'Yes' : 'No'}<br/>Services: ${sh.availableServices}`
        )
        .addTo(group);
    });

    resources.forEach((res) => {
      L.circleMarker([res.warehouseLatitude, res.warehouseLongitude], {
        radius: 8,
        fillColor: '#a855f7',
        color: '#ffffff',
        weight: 2,
        fillOpacity: 0.95
      })
        .bindPopup(
          `<strong>Depot: ${res.warehouseName}</strong><br/>Item: ${res.name}<br/>Available: ${res.availableQuantity} / ${res.totalQuantity} ${res.unit}<br/>Cold-Chain: ${res.supportsColdChain ? 'Yes' : 'No'}`
        )
        .addTo(group);
    });
  }, [requests, shelters, resources, operationalAreas]);

  return (
    <div className="w-full h-[340px] sm:h-[450px] rounded-2xl overflow-hidden border border-white/10 shadow-2xl">
      <div ref={mapContainerRef} className="w-full h-full" />
    </div>
  );
};
