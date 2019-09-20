using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IZone
{
    /// <summary>
    /// id of the zone
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// gets the boundingboxes that make out this zone
    /// </summary>
    List<Rect> BoundingBoxes { get; set; }

    /// <summary>
    /// the behavior that divisions will have whilst assigned to this zone
    /// </summary>
    IZoneBehavior Behavior { get; set; }

    /// <summary>
    /// gets a random point inside of the zone
    /// </summary>
    /// <returns></returns>
    Vector3 GetRandomPoint();

    /// <summary>
    /// check if a zone contains a point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    bool Contains(Vector3 point);

    /// <summary>
    /// check if a zone contains a point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    bool Contains(Vector2 point);

    /// <summary>
    /// check if a zone overlaps another
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    bool Overlaps(Zone other);


}
