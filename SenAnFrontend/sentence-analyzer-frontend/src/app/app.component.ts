import { Component } from '@angular/core';
import {NestedTreeControl} from '@angular/cdk/tree';
import {MatTreeNestedDataSource} from '@angular/material/tree';
import { SelectionModel } from '@angular/cdk/collections';

/**
 * Food data with nested structure.
 * Each node has a name and an optiona list of children.
 */
interface DefaultTreeNode {
  name: string;
  children?: DefaultTreeNode[];
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  treeControl = new NestedTreeControl<DefaultTreeNode>(node => node.children);
  dataSource = new MatTreeNestedDataSource<DefaultTreeNode>();
  treeData: DefaultTreeNode[];


  constructor() {
    this.treeData = [];
    this.dataSource.data = this.treeData;
  }

  hasChild = (_: number, node: DefaultTreeNode) => !!node.children && node.children.length > 0;
  // Base url
  baseurl = 'https://localhost:44388';

  title = 'sentence-analyzer-frontend';

  onSubmit(formData) {
    window.fetch(this.baseurl + '/api/v1/SenAn',{
      method: 'POST', // *GET, POST, PUT, DELETE, etc.
      mode: 'cors', // no-cors, *cors, same-origin
      cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
      headers: {
        'Content-Type': 'application/json'
      },
      redirect: 'follow', // manual, *follow, error
      referrerPolicy: 'no-referrer', // no-referrer, *client
      body: JSON.stringify({fulltext: formData.fulltext}) // body data type must match "Content-Type" header
    }).then(r => r.json()).then(j => { 
      //that's all... no magic, no bloated framework
      this.treeData = [];
      this.convertFromJSONRaw2TreeNode(j['text'], this.treeData);
      //console.log(this.treeData);
      this.dataSource.data = this.treeData; });
  }

  convertFromJSONRaw2TreeNode(o, resultArray) {
    for (var i in o) {
      let singleTree = {name: i};  
        if (o[i] !== null && typeof(o[i])=="object") {
            //going one step down in the object tree!!
            singleTree['children'] = [];
            this.convertFromJSONRaw2TreeNode(o[i], singleTree['children']);
        }
        resultArray.push(singleTree);
    }
  }
}
