using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lookat : MonoBehaviour
{
    public GameObject tgt;
    public GameObject self;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        self.transform.LookAt(tgt.transform);
        self.transform.eulerAngles = new Vector3(
            self.transform.eulerAngles.x,
            self.transform.eulerAngles.y + 45,
            self.transform.eulerAngles.z
        );
    }
}
