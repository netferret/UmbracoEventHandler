# UmbracoEventHandler
Description
Builds structure for events, news and blogs or anything else with the given alias prefix for umbraco.

Why was this created and why is it need?
uDateFoldersy wasnt working as I expected.  Instead of unpicking the various events etc, it was easier to just create my own implimentation from scratch.  This was also to ensure that it was compatible with umkbraco 7.4.3.

Takes alias prefixes for contenttypes and then itterates through them to put the them into the correct day node.

Structure
xxxxFolder -> Year -> Month -> Day -> xxxxItem

These types must exist under the document types and they dont require a template.

For example:
events would be the following document types:

eventFolder - This would be under the permissions of home and precreated.
eventItem - These items would be created under the eventFolder which then would move the item to the day folder on publishing.

This could be improved to take the alias's from another location(i.e. the backend), but because of the time constraints I havent been able test this. 
