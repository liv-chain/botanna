namespace AveManiaBot;

using System.Collections.Generic;

public class Message
{
    public string sender_name { get; set; }
    public long timestamp_ms { get; set; }
    public string content { get; set; }
    public bool is_geoblocked_for_viewer { get; set; }
    public bool is_unsent_image_by_messenger_kid_parent { get; set; }
}

public class ChatData
{
    public List<Message> messages { get; set; }
}
